using Domain.Entities;
using Services.Models;

namespace Services.Logic;

public static class ConflictRules
{
    public static IReadOnlyList<ConflictItem> Evaluate(
        IReadOnlyList<ShiftAssignment> assignments,
        IReadOnlyList<Employee> employees,
        IReadOnlyList<Workplace> workplaces,
        IReadOnlyList<Absence> absences,
        IReadOnlyList<CapacityNeed> needs,
        int year,
        int weekNumber,
        IReadOnlyList<ShiftClosure>? shiftClosures = null)
    {
        var conflicts = new List<ConflictItem>();
        var employeeMap = employees.ToDictionary(e => e.Id);
        var workplaceMap = workplaces.ToDictionary(w => w.Id);
        var closed = (shiftClosures ?? [])
            .GroupBy(c => c.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ShiftName).ToHashSet(StringComparer.OrdinalIgnoreCase));

        // Double booking (overlapping times same day)
        var byEmployeeDay = assignments
            .Where(a => a.EmployeeId.HasValue)
            .GroupBy(a => (a.EmployeeId!.Value, a.Date));

        foreach (var group in byEmployeeDay)
        {
            var list = group.OrderBy(a => a.StartTime).ToList();
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    if (Overlaps(list[i], list[j]))
                    {
                        var name = employeeMap.GetValueOrDefault(group.Key.Item1)?.DisplayName ?? "?";
                        conflicts.Add(new ConflictItem(
                            ConflictSeverity.Blocking,
                            "DOUBLE_BOOKING",
                            $"{name} is dubbel ingepland op {group.Key.Date:dd-MM-yyyy}.",
                            group.Key.Item1,
                            list[i].WorkplaceId,
                            group.Key.Date,
                            list[i].Id));
                    }
                }
            }
        }

        // Approved absence
        foreach (var a in assignments.Where(x => x.EmployeeId.HasValue))
        {
            var hit = absences.FirstOrDefault(ab =>
                ab.EmployeeId == a.EmployeeId &&
                ab.Status == AbsenceStatus.Approved &&
                a.Date >= ab.StartDate &&
                a.Date <= ab.EndDate);

            if (hit != null && string.IsNullOrWhiteSpace(a.OverrideReason))
            {
                var name = employeeMap.GetValueOrDefault(a.EmployeeId!.Value)?.DisplayName ?? "?";
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Blocking,
                    "ABSENCE",
                    $"{name} heeft goedgekeurde afwezigheid op {a.Date:dd-MM-yyyy}.",
                    a.EmployeeId,
                    a.WorkplaceId,
                    a.Date,
                    a.Id));
            }
        }

        // Closed shifts
        foreach (var a in assignments)
        {
            if (!closed.TryGetValue(a.Date, out var shifts)) continue;
            if (!shifts.Contains(a.ShiftName)) continue;
            conflicts.Add(new ConflictItem(
                ConflictSeverity.Blocking,
                "SHIFT_CLOSED",
                $"Shift '{a.ShiftName}' is gesloten op {a.Date:dd-MM-yyyy}.",
                a.EmployeeId,
                a.WorkplaceId,
                a.Date,
                a.Id));
        }

        // Qualification / function requirements
        foreach (var a in assignments.Where(x => x.EmployeeId.HasValue))
        {
            if (!workplaceMap.TryGetValue(a.WorkplaceId, out var wp) || !employeeMap.TryGetValue(a.EmployeeId!.Value, out var emp))
                continue;

            foreach (var req in wp.Requirements)
            {
                if (req.QualificationId.HasValue)
                {
                    var has = emp.Qualifications.Any(q =>
                        q.QualificationId == req.QualificationId &&
                        (q.ValidUntil == null || q.ValidUntil >= a.Date));
                    if (!has)
                    {
                        conflicts.Add(new ConflictItem(
                            ConflictSeverity.Warning,
                            "QUALIFICATION",
                            $"{emp.DisplayName} mist kwalificatie voor {wp.Name}.",
                            emp.Id,
                            wp.Id,
                            a.Date,
                            a.Id));
                    }
                }

                if (req.JobFunctionId.HasValue && emp.PrimaryJobFunctionId != req.JobFunctionId)
                {
                    conflicts.Add(new ConflictItem(
                        ConflictSeverity.Warning,
                        "FUNCTION",
                        $"{emp.DisplayName} heeft niet de vereiste functie voor {wp.Name}.",
                        emp.Id,
                        wp.Id,
                        a.Date,
                        a.Id));
                }
            }
        }

        // BHV validity + coverage per workplace/day
        foreach (var a in assignments.Where(x => x.EmployeeId.HasValue))
        {
            if (!employeeMap.TryGetValue(a.EmployeeId!.Value, out var emp))
                continue;
            if (emp.IsBhv && emp.BhvValidUntil.HasValue && emp.BhvValidUntil < a.Date)
            {
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Warning,
                    "BHV_EXPIRED",
                    $"BHV van {emp.DisplayName} is verlopen op {a.Date:dd-MM-yyyy}.",
                    emp.Id,
                    a.WorkplaceId,
                    a.Date,
                    a.Id));
            }
        }

        var byWpDay = assignments.GroupBy(a => (a.WorkplaceId, a.Date));
        foreach (var group in byWpDay)
        {
            if (!workplaceMap.TryGetValue(group.Key.WorkplaceId, out var wp))
                continue;
            if (wp.RequiredBhvCount <= 0)
                continue;

            var bhvCount = group.Count(a =>
                a.EmployeeId.HasValue &&
                employeeMap.TryGetValue(a.EmployeeId.Value, out var emp) &&
                emp.IsBhv &&
                (emp.BhvValidUntil == null || emp.BhvValidUntil >= group.Key.Date));

            if (bhvCount < wp.RequiredBhvCount)
            {
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Warning,
                    "BHV_COVERAGE",
                    $"{wp.Name} heeft onvoldoende BHV op {group.Key.Date:dd-MM-yyyy} ({bhvCount}/{wp.RequiredBhvCount}).",
                    null,
                    wp.Id,
                    group.Key.Date));
            }
        }

        // Under/over staffing vs capacity needs (week totals)
        var weekNeeds = needs
            .Where(n => n.IsActive && n.Year == year && n.WeekNumber == weekNumber)
            .GroupBy(n => n.WorkplaceId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RequiredHours));

        var plannedByWp = assignments
            .GroupBy(a => a.WorkplaceId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PlannedHours));

        foreach (var wp in workplaces.Where(w => w.Active))
        {
            weekNeeds.TryGetValue(wp.Id, out var required);
            plannedByWp.TryGetValue(wp.Id, out var planned);
            var diff = planned - required;
            if (required > 0 && planned < required)
            {
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Warning,
                    "UNDERSTAFFED",
                    $"{wp.Name}: onderbezetting {diff:0.##} uur (gepland {planned:0.##} / benodigd {required:0.##}).",
                    null,
                    wp.Id));
            }
            else if (required > 0 && planned > required)
            {
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Warning,
                    "OVERSTAFFED",
                    $"{wp.Name}: overbezetting +{diff:0.##} uur (gepland {planned:0.##} / benodigd {required:0.##}).",
                    null,
                    wp.Id));
            }
        }

        // Contract hours deviation
        foreach (var emp in employees.Where(e => e.Active))
        {
            var planned = assignments.Where(a => a.EmployeeId == emp.Id).Sum(a => a.PlannedHours);
            var diff = planned - emp.ContractHoursPerWeek;
            if (Math.Abs(diff) >= 4)
            {
                conflicts.Add(new ConflictItem(
                    ConflictSeverity.Warning,
                    "CONTRACT_HOURS",
                    $"{emp.DisplayName}: contract {emp.ContractHoursPerWeek:0.##}u, gepland {planned:0.##}u ({diff:+0.##;-0.##}).",
                    emp.Id));
            }
        }

        // Rest between shifts (< 11 hours) – consecutive days
        foreach (var empGroup in assignments.Where(a => a.EmployeeId.HasValue).GroupBy(a => a.EmployeeId!.Value))
        {
            var ordered = empGroup.OrderBy(a => a.Date).ThenBy(a => a.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                var prevEnd = prev.Date.ToDateTime(prev.EndTime);
                if (prev.EndTime <= prev.StartTime)
                    prevEnd = prevEnd.AddDays(1);
                var currStart = curr.Date.ToDateTime(curr.StartTime);
                var rest = currStart - prevEnd;
                if (rest < TimeSpan.FromHours(11))
                {
                    var name = employeeMap.GetValueOrDefault(empGroup.Key)?.DisplayName ?? "?";
                    conflicts.Add(new ConflictItem(
                        ConflictSeverity.Warning,
                        "REST_TIME",
                        $"{name}: minder dan 11 uur rust tussen {prev.Date:dd-MM} en {curr.Date:dd-MM}.",
                        empGroup.Key,
                        curr.WorkplaceId,
                        curr.Date,
                        curr.Id));
                }
            }
        }

        return conflicts
            .GroupBy(c => (c.Code, c.Message, c.EmployeeId, c.WorkplaceId, c.Date))
            .Select(g => g.First())
            .ToList();
    }

    private static bool Overlaps(ShiftAssignment a, ShiftAssignment b)
    {
        var aStart = a.Date.ToDateTime(a.StartTime);
        var aEnd = a.Date.ToDateTime(a.EndTime);
        if (a.EndTime <= a.StartTime) aEnd = aEnd.AddDays(1);

        var bStart = b.Date.ToDateTime(b.StartTime);
        var bEnd = b.Date.ToDateTime(b.EndTime);
        if (b.EndTime <= b.StartTime) bEnd = bEnd.AddDays(1);

        return aStart < bEnd && bStart < aEnd;
    }

    public static IReadOnlyList<CapacityRow> BuildCapacity(
        IReadOnlyList<Workplace> workplaces,
        IReadOnlyList<CapacityNeed> needs,
        IReadOnlyList<ShiftAssignment> assignments,
        int year,
        int weekNumber)
    {
        var needByWp = needs
            .Where(n => n.IsActive && n.Year == year && n.WeekNumber == weekNumber)
            .GroupBy(n => n.WorkplaceId)
            .ToDictionary(g => g.Key, g => (Shifts: g.Sum(x => x.RequiredShifts), Hours: g.Sum(x => x.RequiredHours)));

        var planByWp = assignments
            .GroupBy(a => a.WorkplaceId)
            .ToDictionary(g => g.Key, g => (
                Shifts: g.Count(),
                Hours: g.Sum(x => x.PlannedHours),
                People: g.Where(x => x.EmployeeId.HasValue).Select(x => x.EmployeeId!.Value).Distinct().Count()));

        return workplaces.Where(w => w.Active).Select(w =>
        {
            needByWp.TryGetValue(w.Id, out var need);
            planByWp.TryGetValue(w.Id, out var plan);
            return new CapacityRow(
                w.Id,
                w.Name,
                w.ExternalNumber,
                w.Department?.Name,
                need.Shifts,
                need.Hours,
                plan.Shifts,
                plan.Hours,
                plan.Hours - need.Hours,
                plan.People);
        }).ToList();
    }
}
