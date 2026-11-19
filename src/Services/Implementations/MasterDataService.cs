using Domain.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Services.Implementations;

public class MasterDataService(RepositoryContext db, IAuditService audit) : IMasterDataService
{
    public async Task<List<Employee>> GetEmployeesAsync(bool includeInactive = false)
    {
        var q = db.Employees
            .Include(e => e.PrimaryJobFunction)
            .Include(e => e.PrimaryCrew!)
                .ThenInclude(c => c.CrewPattern!)
                .ThenInclude(p => p.Days)
            .Include(e => e.PrimaryWorkplace)
            .Include(e => e.Qualifications).ThenInclude(q => q.Qualification)
            .AsQueryable();
        if (!includeInactive) q = q.Where(e => e.Active);
        return await q.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).AsNoTracking().ToListAsync();
    }

    public Task<Employee?> GetEmployeeAsync(int id) =>
        db.Employees
            .Include(e => e.Qualifications)
            .Include(e => e.CrewMemberships)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Employee> SaveEmployeeAsync(Employee employee, string userName)
    {
        if (await db.Employees.AnyAsync(e => e.PersonnelNumber == employee.PersonnelNumber && e.Id != employee.Id))
            throw new InvalidOperationException("Personeelsnummer is al in gebruik.");

        if (employee.Id == 0)
        {
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Employee), employee.Id, "Create", null, employee.PersonnelNumber);
        }
        else
        {
            var existing = await db.Employees.FindAsync(employee.Id)
                ?? throw new InvalidOperationException("Medewerker niet gevonden.");
            var old = $"{existing.FirstName} {existing.LastName}|{existing.PrimaryCrewId}|{existing.ContractHoursPerWeek}";
            existing.PersonnelNumber = employee.PersonnelNumber;
            existing.FirstName = employee.FirstName;
            existing.LastName = employee.LastName;
            existing.PrimaryJobFunctionId = employee.PrimaryJobFunctionId;
            existing.Active = employee.Active;
            existing.IsBhv = employee.IsBhv;
            existing.BhvValidUntil = employee.BhvValidUntil;
            existing.ContractHoursPerWeek = employee.ContractHoursPerWeek;
            existing.Fte = employee.Fte;
            existing.PrimaryCrewId = employee.PrimaryCrewId;
            existing.Remarks = employee.Remarks;
            existing.EmploymentStart = employee.EmploymentStart;
            existing.EmploymentEnd = employee.EmploymentEnd;
            existing.PrimaryWorkplaceId = employee.PrimaryWorkplaceId;
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Employee), existing.Id, "Update", old,
                $"{existing.FirstName} {existing.LastName}|{existing.PrimaryCrewId}|{existing.ContractHoursPerWeek}");
            employee = existing;
        }

        return employee;
    }

    public async Task SetEmployeeQualificationsAsync(int employeeId, IEnumerable<int> qualificationIds, string userName)
    {
        var existing = await db.EmployeeQualifications.Where(q => q.EmployeeId == employeeId).ToListAsync();
        db.EmployeeQualifications.RemoveRange(existing);
        foreach (var qid in qualificationIds.Distinct())
        {
            db.EmployeeQualifications.Add(new EmployeeQualification
            {
                EmployeeId = employeeId,
                QualificationId = qid
            });
        }
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(EmployeeQualification), employeeId, "Replace", null, string.Join(",", qualificationIds));
    }

    public Task<List<JobFunction>> GetJobFunctionsAsync() =>
        db.JobFunctions.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

    public async Task<JobFunction> SaveJobFunctionAsync(JobFunction item)
    {
        if (item.Id == 0) db.JobFunctions.Add(item);
        else db.JobFunctions.Update(item);
        await db.SaveChangesAsync();
        return item;
    }

    public Task<List<Qualification>> GetQualificationsAsync() =>
        db.Qualifications.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

    public async Task<Qualification> SaveQualificationAsync(Qualification item)
    {
        if (item.Id == 0) db.Qualifications.Add(item);
        else db.Qualifications.Update(item);
        await db.SaveChangesAsync();
        return item;
    }

    public Task<List<Department>> GetDepartmentsAsync() =>
        db.Departments.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

    public async Task<Department> SaveDepartmentAsync(Department item)
    {
        if (item.Id == 0) db.Departments.Add(item);
        else db.Departments.Update(item);
        await db.SaveChangesAsync();
        return item;
    }

    public Task<List<Crew>> GetCrewsAsync() =>
        db.Crews.Include(c => c.CrewPattern).OrderBy(c => c.Name).AsNoTracking().ToListAsync();

    public async Task<Crew> SaveCrewAsync(Crew item)
    {
        if (item.Id == 0) db.Crews.Add(item);
        else
        {
            var existing = await db.Crews.FindAsync(item.Id) ?? throw new InvalidOperationException("Ploeg niet gevonden.");
            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.CrewPatternId = item.CrewPatternId;
            existing.EffectiveFrom = item.EffectiveFrom;
            existing.EffectiveTo = item.EffectiveTo;
            existing.Active = item.Active;
            item = existing;
        }
        await db.SaveChangesAsync();
        return item;
    }

    public Task<List<CrewPattern>> GetCrewPatternsAsync() =>
        db.CrewPatterns.Include(p => p.Days).OrderBy(p => p.Name).AsNoTracking().ToListAsync();

    public async Task<CrewPattern> SaveCrewPatternAsync(CrewPattern item)
    {
        if (item.Id == 0) db.CrewPatterns.Add(item);
        else
        {
            var existing = await db.CrewPatterns.FindAsync(item.Id) ?? throw new InvalidOperationException("Patroon niet gevonden.");
            existing.Name = item.Name;
            existing.CycleWeeks = item.CycleWeeks;
            existing.EffectiveFrom = item.EffectiveFrom;
            existing.EffectiveTo = item.EffectiveTo;
            existing.Active = item.Active;
            item = existing;
        }
        await db.SaveChangesAsync();
        return item;
    }

    public async Task SavePatternDaysAsync(int patternId, IEnumerable<PatternDay> days)
    {
        var existing = await db.PatternDays.Where(d => d.CrewPatternId == patternId).ToListAsync();
        db.PatternDays.RemoveRange(existing);
        foreach (var day in days)
        {
            day.Id = 0;
            day.CrewPatternId = patternId;
            db.PatternDays.Add(day);
        }
        await db.SaveChangesAsync();
    }

    public Task<List<Workplace>> GetWorkplacesAsync(bool includeInactive = false)
    {
        var q = db.Workplaces
            .Include(w => w.Department)
            .Include(w => w.Requirements).ThenInclude(r => r.JobFunction)
            .Include(w => w.Requirements).ThenInclude(r => r.Qualification)
            .AsQueryable();
        if (!includeInactive) q = q.Where(w => w.Active);
        return q.OrderBy(w => w.ExternalNumber).AsNoTracking().ToListAsync();
    }

    public Task<Workplace?> GetWorkplaceAsync(int id) =>
        db.Workplaces.Include(w => w.Requirements).FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Workplace> SaveWorkplaceAsync(Workplace item, string userName)
    {
        if (await db.Workplaces.AnyAsync(w => w.ExternalNumber == item.ExternalNumber && w.Id != item.Id))
            throw new InvalidOperationException("Werkpleknummer is al in gebruik.");

        if (item.Id == 0)
        {
            db.Workplaces.Add(item);
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Workplace), item.Id, "Create", null, item.ExternalNumber);
        }
        else
        {
            var existing = await db.Workplaces.FindAsync(item.Id) ?? throw new InvalidOperationException("Werkplek niet gevonden.");
            existing.ExternalNumber = item.ExternalNumber;
            existing.Name = item.Name;
            existing.DepartmentId = item.DepartmentId;
            existing.Description = item.Description;
            existing.Active = item.Active;
            existing.MinimumStaffing = item.MinimumStaffing;
            existing.RequiredBhvCount = item.RequiredBhvCount;
            existing.Remarks = item.Remarks;
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Workplace), existing.Id, "Update", null, existing.ExternalNumber);
            item = existing;
        }
        return item;
    }

    public async Task SaveWorkplaceRequirementsAsync(int workplaceId, IEnumerable<WorkplaceRequirement> requirements)
    {
        var existing = await db.WorkplaceRequirements.Where(r => r.WorkplaceId == workplaceId).ToListAsync();
        db.WorkplaceRequirements.RemoveRange(existing);
        foreach (var r in requirements)
        {
            r.Id = 0;
            r.WorkplaceId = workplaceId;
            db.WorkplaceRequirements.Add(r);
        }
        await db.SaveChangesAsync();
    }

    public Task<List<CalendarDay>> GetCalendarDaysAsync(int year) =>
        db.CalendarDays.Where(c => c.Date.Year == year).OrderBy(c => c.Date).AsNoTracking().ToListAsync();

    public async Task UpsertCalendarDayAsync(DateOnly date, CalendarDayType type, string? remark)
    {
        var day = await db.CalendarDays.FirstOrDefaultAsync(c => c.Date == date);
        if (day is null)
        {
            db.CalendarDays.Add(new CalendarDay { Date = date, Type = type, Remark = remark });
        }
        else
        {
            day.Type = type;
            day.Remark = remark;
        }
        await db.SaveChangesAsync();
    }

    public Task<List<WorkplaceCalendarException>> GetWorkplaceCalendarExceptionsAsync(int? workplaceId = null, int? year = null)
    {
        var q = db.WorkplaceCalendarExceptions.Include(e => e.Workplace).AsQueryable();
        if (workplaceId.HasValue) q = q.Where(e => e.WorkplaceId == workplaceId);
        if (year.HasValue)
        {
            var from = new DateOnly(year.Value, 1, 1);
            var to = new DateOnly(year.Value, 12, 31);
            q = q.Where(e => e.Date >= from && e.Date <= to);
        }

        return q.OrderBy(e => e.Date).ThenBy(e => e.Workplace!.ExternalNumber).AsNoTracking().ToListAsync();
    }

    public async Task UpsertWorkplaceCalendarExceptionAsync(int workplaceId, DateOnly date, CalendarDayType type, string? remark)
    {
        var existing = await db.WorkplaceCalendarExceptions
            .FirstOrDefaultAsync(e => e.WorkplaceId == workplaceId && e.Date == date);
        if (existing is null)
        {
            db.WorkplaceCalendarExceptions.Add(new WorkplaceCalendarException
            {
                WorkplaceId = workplaceId,
                Date = date,
                Type = type,
                Remark = remark
            });
        }
        else
        {
            existing.Type = type;
            existing.Remark = remark;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteWorkplaceCalendarExceptionAsync(int id)
    {
        var item = await db.WorkplaceCalendarExceptions.FindAsync(id)
            ?? throw new InvalidOperationException("Uitzondering niet gevonden.");
        db.WorkplaceCalendarExceptions.Remove(item);
        await db.SaveChangesAsync();
    }

    public Task<List<ShiftClosure>> GetShiftClosuresAsync(DateOnly from, DateOnly to) =>
        db.ShiftClosures
            .Where(c => c.Date >= from && c.Date <= to)
            .OrderBy(c => c.Date).ThenBy(c => c.ShiftName)
            .AsNoTracking()
            .ToListAsync();

    public async Task SetShiftClosuresForDateAsync(DateOnly date, IEnumerable<string> closedShiftNames, string? remark = null)
    {
        var wanted = closedShiftNames
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await db.ShiftClosures.Where(c => c.Date == date).ToListAsync();
        db.ShiftClosures.RemoveRange(existing.Where(e => !wanted.Contains(e.ShiftName)));

        var existingNames = existing.Select(e => e.ShiftName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in wanted)
        {
            if (existingNames.Contains(name)) continue;
            db.ShiftClosures.Add(new ShiftClosure
            {
                Date = date,
                ShiftName = name,
                Remark = remark
            });
        }

        await db.SaveChangesAsync();
    }

    public Task<List<Absence>> GetAbsencesAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var q = db.Absences.Include(a => a.Employee).AsQueryable();
        if (from.HasValue) q = q.Where(a => a.EndDate >= from);
        if (to.HasValue) q = q.Where(a => a.StartDate <= to);
        return q.OrderByDescending(a => a.StartDate).AsNoTracking().ToListAsync();
    }

    public async Task<Absence> SaveAbsenceAsync(Absence absence, string userName)
    {
        if (absence.Id == 0)
        {
            absence.CreatedBy = userName;
            absence.CreatedAt = DateTime.UtcNow;
            db.Absences.Add(absence);
        }
        else
        {
            var existing = await db.Absences.FindAsync(absence.Id) ?? throw new InvalidOperationException("Afwezigheid niet gevonden.");
            existing.EmployeeId = absence.EmployeeId;
            existing.StartDate = absence.StartDate;
            existing.EndDate = absence.EndDate;
            existing.Type = absence.Type;
            existing.Status = absence.Status;
            existing.Remark = absence.Remark;
            absence = existing;
        }
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(Absence), absence.Id, "Save", null, $"{absence.EmployeeId}:{absence.StartDate}-{absence.EndDate}:{absence.Status}");
        return absence;
    }

    public async Task RemoveAbsenceForDayAsync(int employeeId, DateOnly date, string userName)
    {
        var absence = await db.Absences.FirstOrDefaultAsync(a =>
            a.EmployeeId == employeeId &&
            a.Status == AbsenceStatus.Approved &&
            date >= a.StartDate &&
            date <= a.EndDate)
            ?? throw new InvalidOperationException("Geen goedgekeurd verlof gevonden op deze dag.");

        var old = $"{absence.StartDate}-{absence.EndDate}";

        if (absence.StartDate == date && absence.EndDate == date)
        {
            db.Absences.Remove(absence);
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Absence), absence.Id, "DeleteDay", old, date.ToString("dd-MM-yyyy"));
            return;
        }

        if (absence.StartDate == date)
        {
            absence.StartDate = date.AddDays(1);
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Absence), absence.Id, "ShrinkStart", old, $"{absence.StartDate}-{absence.EndDate}");
            return;
        }

        if (absence.EndDate == date)
        {
            absence.EndDate = date.AddDays(-1);
            await db.SaveChangesAsync();
            await audit.LogAsync(userName, nameof(Absence), absence.Id, "ShrinkEnd", old, $"{absence.StartDate}-{absence.EndDate}");
            return;
        }

        // Middle day: split into before + after
        var after = new Absence
        {
            EmployeeId = absence.EmployeeId,
            StartDate = date.AddDays(1),
            EndDate = absence.EndDate,
            Type = absence.Type,
            Status = AbsenceStatus.Approved,
            Remark = absence.Remark,
            CreatedBy = userName,
            CreatedAt = DateTime.UtcNow
        };
        absence.EndDate = date.AddDays(-1);
        db.Absences.Add(after);
        await db.SaveChangesAsync();
        await audit.LogAsync(userName, nameof(Absence), absence.Id, "SplitRemoveDay", old, $"removed {date:dd-MM-yyyy}");
    }
}
