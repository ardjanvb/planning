using System.Text.Json;
using Domain;
using Domain.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Logic;
using Services.Models;

namespace Services.Implementations;

public class PlanningService(RepositoryContext db, IAuditService audit) : IPlanningService
{
    public async Task<WeekPlan> GetOrCreateWeekPlanAsync(int year, int weekNumber)
    {
        var plan = await db.WeekPlans.FirstOrDefaultAsync(p => p.Year == year && p.WeekNumber == weekNumber);
        if (plan is not null) return plan;

        plan = new WeekPlan { Year = year, WeekNumber = weekNumber, Status = WeekPlanStatus.Concept, Version = 1 };
        db.WeekPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public async Task<PlanningOverview> GetOverviewAsync(int year, int weekNumber)
    {
        var plan = await GetOrCreateWeekPlanAsync(year, weekNumber);
        var days = WeekHelper.DaysOfIsoWeek(year, weekNumber);

        var workplaces = await db.Workplaces
            .Include(w => w.Department)
            .Include(w => w.Requirements)
            .Where(w => w.Active)
            .AsNoTracking()
            .ToListAsync();

        var employees = await db.Employees
            .Include(e => e.Qualifications)
            .Where(e => e.Active)
            .AsNoTracking()
            .ToListAsync();

        var assignments = await db.ShiftAssignments
            .Include(a => a.Employee)
            .Include(a => a.Workplace)
            .Where(a => a.WeekPlanId == plan.Id)
            .AsNoTracking()
            .ToListAsync();

        var needs = await db.CapacityNeeds
            .Where(n => n.IsActive && n.Year == year && n.WeekNumber == weekNumber)
            .AsNoTracking()
            .ToListAsync();

        var absences = await db.Absences
            .Where(a => a.EndDate >= days.First() && a.StartDate <= days.Last())
            .AsNoTracking()
            .ToListAsync();

        var closures = await db.ShiftClosures
            .Where(c => c.Date >= days.First() && c.Date <= days.Last())
            .AsNoTracking()
            .ToListAsync();

        var closedByDate = days.ToDictionary(
            d => d,
            d => (IReadOnlyList<string>)closures
                .Where(c => c.Date == d)
                .Select(c => c.ShiftName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());

        var capacity = ConflictRules.BuildCapacity(workplaces, needs, assignments, year, weekNumber);
        var conflicts = ConflictRules.Evaluate(assignments, employees, workplaces, absences, needs, year, weekNumber, closures);

        var needHoursByWp = needs.GroupBy(n => n.WorkplaceId).ToDictionary(g => g.Key, g => g.Sum(x => x.RequiredHours));

        var byWorkplace = workplaces.Select(wp =>
        {
            var dayMap = days.ToDictionary(d => d, d =>
            {
                var dayAssignments = assignments.Where(a => a.WorkplaceId == wp.Id && a.Date == d).ToList();
                return new WeekMatrixCell(
                    d,
                    dayAssignments.Select(ToDto).ToList(),
                    0,
                    dayAssignments.Sum(a => a.PlannedHours),
                    false,
                    closedByDate[d]);
            });
            needHoursByWp.TryGetValue(wp.Id, out var required);
            return new WorkplaceWeekRow(
                wp.Id,
                wp.Name,
                wp.ExternalNumber,
                dayMap,
                required,
                assignments.Where(a => a.WorkplaceId == wp.Id).Sum(a => a.PlannedHours));
        }).ToList();

        var byEmployee = employees.Select(emp =>
        {
            var dayMap = days.ToDictionary(d => d, d =>
            {
                var leave = absences.FirstOrDefault(a =>
                    a.EmployeeId == emp.Id &&
                    a.Status == AbsenceStatus.Approved &&
                    d >= a.StartDate &&
                    d <= a.EndDate);
                var onLeave = leave is not null;
                var dayAssignments = assignments.Where(a => a.EmployeeId == emp.Id && a.Date == d).ToList();
                return new WeekMatrixCell(
                    d,
                    dayAssignments.Select(ToDto).ToList(),
                    0,
                    dayAssignments.Sum(a => a.PlannedHours),
                    onLeave,
                    closedByDate[d],
                    leave?.Id);
            });
            var hasAbsence = absences.Any(a =>
                a.EmployeeId == emp.Id &&
                a.Status == AbsenceStatus.Approved &&
                a.StartDate <= days.Last() &&
                a.EndDate >= days.First());
            return new EmployeeWeekRow(
                emp.Id,
                emp.DisplayName,
                emp.ContractHoursPerWeek,
                dayMap,
                assignments.Where(a => a.EmployeeId == emp.Id).Sum(a => a.PlannedHours),
                hasAbsence);
        }).ToList();

        return new PlanningOverview(plan, days, byWorkplace, byEmployee, capacity, conflicts, closedByDate);
    }

    public async Task<ShiftAssignment> AssignAsync(AssignRequest request, string userName)
    {
        var plan = await db.WeekPlans.FindAsync(request.WeekPlanId)
            ?? throw new InvalidOperationException("Weekplan niet gevonden.");
        EnsureEditable(plan);
        await EnsureAssignableAsync(request.EmployeeId, request.Date, request.ShiftName);

        var assignment = new ShiftAssignment
        {
            WeekPlanId = request.WeekPlanId,
            Date = request.Date,
            WorkplaceId = request.WorkplaceId,
            EmployeeId = request.EmployeeId,
            ShiftName = request.ShiftName,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            PlannedHours = request.PlannedHours,
            OverrideReason = request.OverrideReason
        };
        db.ShiftAssignments.Add(assignment);
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(ShiftAssignment), assignment.Id, "Assign", null,
            $"{request.EmployeeId}@{request.WorkplaceId}/{request.Date}", request.OverrideReason);
        return assignment;
    }

    public async Task RemoveAssignmentAsync(int assignmentId, string userName)
    {
        var assignment = await db.ShiftAssignments.Include(a => a.WeekPlan).FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException("Toewijzing niet gevonden.");
        EnsureEditable(assignment.WeekPlan!);
        db.ShiftAssignments.Remove(assignment);
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(ShiftAssignment), assignmentId, "Remove",
            $"{assignment.EmployeeId}@{assignment.WorkplaceId}/{assignment.Date}", null);
    }

    public async Task MoveAssignmentAsync(int assignmentId, int newWorkplaceId, DateOnly? newDate, string userName)
    {
        var assignment = await db.ShiftAssignments.Include(a => a.WeekPlan).FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException("Toewijzing niet gevonden.");
        EnsureEditable(assignment.WeekPlan!);

        var targetDate = newDate ?? assignment.Date;
        if (assignment.EmployeeId.HasValue)
            await EnsureAssignableAsync(assignment.EmployeeId.Value, targetDate, assignment.ShiftName);

        var old = $"{assignment.WorkplaceId}/{assignment.Date}";
        assignment.WorkplaceId = newWorkplaceId;
        if (newDate.HasValue) assignment.Date = newDate.Value;
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(ShiftAssignment), assignmentId, "Move", old, $"{newWorkplaceId}/{assignment.Date}");
    }

    public async Task CopyWeekAsync(int fromYear, int fromWeek, int toYear, int toWeek, string userName)
    {
        var from = await GetOrCreateWeekPlanAsync(fromYear, fromWeek);
        var to = await GetOrCreateWeekPlanAsync(toYear, toWeek);
        EnsureEditable(to);

        var source = await db.ShiftAssignments.Where(a => a.WeekPlanId == from.Id).AsNoTracking().ToListAsync();
        var fromDays = WeekHelper.DaysOfIsoWeek(fromYear, fromWeek).ToList();
        var toDays = WeekHelper.DaysOfIsoWeek(toYear, toWeek).ToList();

        var absences = await db.Absences
            .Where(a => a.Status == AbsenceStatus.Approved && a.EndDate >= toDays.First() && a.StartDate <= toDays.Last())
            .AsNoTracking()
            .ToListAsync();
        var closures = await db.ShiftClosures
            .Where(c => c.Date >= toDays.First() && c.Date <= toDays.Last())
            .AsNoTracking()
            .ToListAsync();

        var existing = await db.ShiftAssignments.Where(a => a.WeekPlanId == to.Id).ToListAsync();
        db.ShiftAssignments.RemoveRange(existing);

        foreach (var a in source)
        {
            var offset = fromDays.IndexOf(a.Date);
            if (offset < 0 || offset >= toDays.Count) continue;
            var targetDate = toDays[offset];

            if (a.EmployeeId.HasValue &&
                absences.Any(ab => ab.EmployeeId == a.EmployeeId && targetDate >= ab.StartDate && targetDate <= ab.EndDate))
                continue;

            if (closures.Any(c => c.Date == targetDate &&
                                  string.Equals(c.ShiftName, a.ShiftName, StringComparison.OrdinalIgnoreCase)))
                continue;

            db.ShiftAssignments.Add(new ShiftAssignment
            {
                WeekPlanId = to.Id,
                Date = targetDate,
                WorkplaceId = a.WorkplaceId,
                EmployeeId = a.EmployeeId,
                ShiftName = a.ShiftName,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                PlannedHours = a.PlannedHours
            });
        }
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(WeekPlan), to.Id, "CopyWeek", $"{fromYear}-W{fromWeek}", $"{toYear}-W{toWeek}");
    }

    public async Task<WeekPlan> ChangeStatusAsync(int weekPlanId, WeekPlanStatus newStatus, string userName, string? reason = null)
    {
        var plan = await db.WeekPlans.Include(p => p.Assignments).FirstOrDefaultAsync(p => p.Id == weekPlanId)
            ?? throw new InvalidOperationException("Weekplan niet gevonden.");

        var overview = await GetOverviewAsync(plan.Year, plan.WeekNumber);
        if (newStatus is WeekPlanStatus.PendingApproval or WeekPlanStatus.Approved or WeekPlanStatus.Published)
        {
            var blocking = overview.Conflicts.Where(c => c.Severity == ConflictSeverity.Blocking).ToList();
            if (blocking.Count > 0)
                throw new InvalidOperationException($"Er zijn {blocking.Count} blokkerende conflicten. Los deze eerst op of registreer een uitzondering.");
        }

        var old = plan.Status.ToString();
        plan.Status = newStatus;
        if (newStatus == WeekPlanStatus.Published)
        {
            plan.PublishedAt = DateTime.UtcNow;
            plan.PublishedBy = userName;
            plan.Version += 1;

            var workplaceNames = await db.Workplaces.AsNoTracking()
                .ToDictionaryAsync(w => w.Id, w => $"{w.ExternalNumber} {w.Name}");
            var employeeNames = await db.Employees.AsNoTracking()
                .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());

            db.WeekPlanSnapshots.Add(new WeekPlanSnapshot
            {
                WeekPlanId = plan.Id,
                Version = plan.Version,
                Status = plan.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName,
                PayloadJson = JsonSerializer.Serialize(plan.Assignments.Select(a => new SnapshotAssignmentDto(
                    a.Date,
                    a.WorkplaceId,
                    workplaceNames.GetValueOrDefault(a.WorkplaceId, $"#{a.WorkplaceId}"),
                    a.EmployeeId,
                    a.EmployeeId is int eid ? employeeNames.GetValueOrDefault(eid) : null,
                    a.ShiftName,
                    a.StartTime,
                    a.EndTime,
                    a.PlannedHours)))
            });
        }

        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(WeekPlan), plan.Id, "Status", old, newStatus.ToString(), reason);
        return plan;
    }

    public async Task<IReadOnlyList<ConflictItem>> GetConflictsAsync(int year, int weekNumber)
    {
        var overview = await GetOverviewAsync(year, weekNumber);
        return overview.Conflicts;
    }

    public async Task<IReadOnlyList<WeekPlanSnapshotInfo>> GetSnapshotsAsync(int year, int weekNumber)
    {
        var plan = await db.WeekPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Year == year && p.WeekNumber == weekNumber);
        if (plan is null) return [];

        var snaps = await db.WeekPlanSnapshots.AsNoTracking()
            .Where(s => s.WeekPlanId == plan.Id)
            .OrderByDescending(s => s.Version)
            .ToListAsync();

        return snaps.Select(s =>
        {
            var count = 0;
            try
            {
                count = JsonSerializer.Deserialize<List<JsonElement>>(s.PayloadJson)?.Count ?? 0;
            }
            catch { /* ignore corrupt payload */ }

            return new WeekPlanSnapshotInfo(
                s.Id, plan.Id, plan.Year, plan.WeekNumber, s.Version, s.Status, s.CreatedAt, s.CreatedBy, count);
        }).ToList();
    }

    public async Task<IReadOnlyList<SnapshotAssignmentDto>> GetSnapshotAssignmentsAsync(int snapshotId)
    {
        var snap = await db.WeekPlanSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId)
            ?? throw new InvalidOperationException("Snapshot niet gevonden.");
        return await HydrateSnapshotAssignmentsAsync(snap.PayloadJson);
    }

    public async Task<VersionCompareResult> CompareSnapshotsAsync(int leftSnapshotId, int rightSnapshotId)
    {
        var left = await db.WeekPlanSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == leftSnapshotId)
            ?? throw new InvalidOperationException("Linker snapshot niet gevonden.");
        var right = await db.WeekPlanSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == rightSnapshotId)
            ?? throw new InvalidOperationException("Rechter snapshot niet gevonden.");
        var plan = await db.WeekPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == left.WeekPlanId)
            ?? throw new InvalidOperationException("Weekplan niet gevonden.");

        var leftItems = await HydrateSnapshotAssignmentsAsync(left.PayloadJson);
        var rightItems = await HydrateSnapshotAssignmentsAsync(right.PayloadJson);

        static string Key(SnapshotAssignmentDto a) =>
            $"{a.Date:yyyy-MM-dd}|{a.WorkplaceId}|{a.EmployeeId}|{a.ShiftName}";

        var leftMap = leftItems.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());
        var rightMap = rightItems.GroupBy(Key).ToDictionary(g => g.Key, g => g.First());

        var onlyLeft = leftMap.Where(kv => !rightMap.ContainsKey(kv.Key)).Select(kv => kv.Value).OrderBy(a => a.Date).ThenBy(a => a.WorkplaceName).ToList();
        var onlyRight = rightMap.Where(kv => !leftMap.ContainsKey(kv.Key)).Select(kv => kv.Value).OrderBy(a => a.Date).ThenBy(a => a.WorkplaceName).ToList();
        var unchanged = leftMap.Where(kv => rightMap.ContainsKey(kv.Key)).Select(kv => kv.Value).OrderBy(a => a.Date).ThenBy(a => a.WorkplaceName).ToList();

        WeekPlanSnapshotInfo Info(WeekPlanSnapshot s) => new(
            s.Id, plan.Id, plan.Year, plan.WeekNumber, s.Version, s.Status, s.CreatedAt, s.CreatedBy,
            s.Id == left.Id ? leftItems.Count : rightItems.Count);

        return new VersionCompareResult(Info(left), Info(right), onlyLeft, onlyRight, unchanged);
    }

    private async Task<IReadOnlyList<SnapshotAssignmentDto>> HydrateSnapshotAssignmentsAsync(string payloadJson)
    {
        List<JsonElement>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<JsonElement>>(payloadJson);
        }
        catch
        {
            return [];
        }

        if (raw is null || raw.Count == 0) return [];

        var workplaceNames = await db.Workplaces.AsNoTracking()
            .ToDictionaryAsync(w => w.Id, w => $"{w.ExternalNumber} {w.Name}");
        var employeeNames = await db.Employees.AsNoTracking()
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());

        static DateOnly ReadDate(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String) return DateOnly.Parse(el.GetString()!);
            // System.Text.Json sometimes writes DateOnly as object { Year, Month, Day }
            if (el.TryGetProperty("Year", out var y))
                return new DateOnly(y.GetInt32(), el.GetProperty("Month").GetInt32(), el.GetProperty("Day").GetInt32());
            return DateOnly.Parse(el.ToString());
        }

        static TimeOnly ReadTime(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String) return TimeOnly.Parse(el.GetString()!);
            if (el.TryGetProperty("Hour", out var h))
                return new TimeOnly(h.GetInt32(), el.GetProperty("Minute").GetInt32(),
                    el.TryGetProperty("Second", out var s) ? s.GetInt32() : 0);
            return TimeOnly.Parse(el.ToString());
        }

        var list = new List<SnapshotAssignmentDto>(raw.Count);
        foreach (var el in raw)
        {
            var date = ReadDate(el.GetProperty("Date"));
            var workplaceId = el.GetProperty("WorkplaceId").GetInt32();
            int? employeeId = el.TryGetProperty("EmployeeId", out var empEl) && empEl.ValueKind != JsonValueKind.Null
                ? empEl.GetInt32()
                : null;
            var shift = el.GetProperty("ShiftName").GetString() ?? "Ochtend";
            var start = ReadTime(el.GetProperty("StartTime"));
            var end = ReadTime(el.GetProperty("EndTime"));
            var hours = el.TryGetProperty("PlannedHours", out var ph) ? ph.GetDecimal() : 8m;

            var wpName = el.TryGetProperty("WorkplaceName", out var wn) && wn.ValueKind == JsonValueKind.String
                ? wn.GetString()!
                : workplaceNames.GetValueOrDefault(workplaceId, $"#{workplaceId}");
            var empName = el.TryGetProperty("EmployeeName", out var en) && en.ValueKind == JsonValueKind.String
                ? en.GetString()
                : employeeId is int eid ? employeeNames.GetValueOrDefault(eid) : null;

            list.Add(new SnapshotAssignmentDto(date, workplaceId, wpName, employeeId, empName, shift, start, end, hours));
        }

        return list.OrderBy(a => a.Date).ThenBy(a => a.WorkplaceName).ThenBy(a => a.ShiftName).ToList();
    }

    private async Task EnsureAssignableAsync(int employeeId, DateOnly date, string shiftName)
    {
        var onLeave = await db.Absences.AnyAsync(a =>
            a.EmployeeId == employeeId &&
            a.Status == AbsenceStatus.Approved &&
            date >= a.StartDate &&
            date <= a.EndDate);
        if (onLeave)
            throw new InvalidOperationException("Medewerker heeft goedgekeurd verlof op deze dag en kan niet worden ingepland.");

        var closedThatDay = await db.ShiftClosures
            .Where(c => c.Date == date)
            .Select(c => c.ShiftName)
            .ToListAsync();
        if (closedThatDay.Any(s => string.Equals(s, shiftName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Shift '{shiftName}' is gesloten op {date:dd-MM-yyyy}.");

        if (PlanningConstants.StandardShifts.All(s =>
                closedThatDay.Any(c => string.Equals(c, s, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException($"Alle shifts zijn gesloten op {date:dd-MM-yyyy}.");
    }

    private static void EnsureEditable(WeekPlan plan)
    {
        if (plan.Status is WeekPlanStatus.Locked)
            throw new InvalidOperationException("Dit weekplan is geblokkeerd.");
    }

    private static ShiftAssignmentDto ToDto(ShiftAssignment a) =>
        new(a.Id, a.Date, a.WorkplaceId, a.Workplace?.Name ?? "", a.ShiftName, a.StartTime, a.EndTime,
            a.EmployeeId, a.Employee?.DisplayName, a.PlannedHours, a.OverrideReason);
}
