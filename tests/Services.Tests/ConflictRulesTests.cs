using Domain.Entities;
using Services.Logic;

namespace Services.Tests;

public class ConflictRulesTests
{
    [Fact]
    public void Detects_double_booking()
    {
        var emp = new Employee { Id = 1, FirstName = "Jan", LastName = "Jansen" };
        var wp = new Workplace { Id = 1, Name = "WP1", ExternalNumber = "WP-01", Active = true };
        var assignments = new List<ShiftAssignment>
        {
            new() { Id = 1, EmployeeId = 1, WorkplaceId = 1, Date = new DateOnly(2026, 11, 23), StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), PlannedHours = 8 },
            new() { Id = 2, EmployeeId = 1, WorkplaceId = 1, Date = new DateOnly(2026, 11, 23), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(18, 0), PlannedHours = 8 }
        };

        var conflicts = ConflictRules.Evaluate(assignments, [emp], [wp], [], [], 2026, 48);

        Assert.Contains(conflicts, c => c.Code == "DOUBLE_BOOKING" && c.Severity == ConflictSeverity.Blocking);
    }

    [Fact]
    public void Detects_approved_absence_without_override()
    {
        var emp = new Employee { Id = 1, FirstName = "Piet", LastName = "P" };
        var wp = new Workplace { Id = 1, Name = "WP1", ExternalNumber = "WP-01", Active = true };
        var assignments = new List<ShiftAssignment>
        {
            new() { Id = 1, EmployeeId = 1, WorkplaceId = 1, Date = new DateOnly(2026, 11, 24), StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), PlannedHours = 8 }
        };
        var absences = new List<Absence>
        {
            new() { EmployeeId = 1, StartDate = new DateOnly(2026, 11, 24), EndDate = new DateOnly(2026, 11, 24), Status = AbsenceStatus.Approved, Type = AbsenceType.Vacation }
        };

        var conflicts = ConflictRules.Evaluate(assignments, [emp], [wp], absences, [], 2026, 48);

        Assert.Contains(conflicts, c => c.Code == "ABSENCE" && c.Severity == ConflictSeverity.Blocking);
    }

    [Fact]
    public void BuildCapacity_computes_difference()
    {
        var wp = new Workplace { Id = 1, Name = "WP1", ExternalNumber = "WP-01", Active = true };
        var needs = new List<CapacityNeed>
        {
            new() { WorkplaceId = 1, Year = 2026, WeekNumber = 48, RequiredShifts = 5, HoursPerShift = 8, IsActive = true }
        };
        var assignments = new List<ShiftAssignment>
        {
            new() { WorkplaceId = 1, EmployeeId = 1, PlannedHours = 8 },
            new() { WorkplaceId = 1, EmployeeId = 2, PlannedHours = 8 },
            new() { WorkplaceId = 1, EmployeeId = 1, PlannedHours = 8 },
            new() { WorkplaceId = 1, EmployeeId = 2, PlannedHours = 8 }
        };

        var rows = ConflictRules.BuildCapacity([wp], needs, assignments, 2026, 48);

        Assert.Single(rows);
        Assert.Equal(40, rows[0].RequiredHours);
        Assert.Equal(32, rows[0].PlannedHours);
        Assert.Equal(-8, rows[0].DifferenceHours);
    }
}
