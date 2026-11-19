using Domain.Entities;
using Services.Logic;

namespace Services.Tests;

public class ConflictRulesShiftClosureTests
{
    [Fact]
    public void Detects_assignment_on_closed_shift()
    {
        var emp = new Employee { Id = 1, FirstName = "Jan", LastName = "J" };
        var wp = new Workplace { Id = 1, Name = "CVS1", ExternalNumber = "CVS1", Active = true };
        var assignments = new List<ShiftAssignment>
        {
            new()
            {
                Id = 1,
                EmployeeId = 1,
                WorkplaceId = 1,
                Date = new DateOnly(2026, 11, 20),
                ShiftName = "Nacht",
                StartTime = new TimeOnly(22, 0),
                EndTime = new TimeOnly(6, 0),
                PlannedHours = 8
            }
        };
        var closures = new List<ShiftClosure>
        {
            new() { Date = new DateOnly(2026, 11, 20), ShiftName = "Nacht" }
        };

        var conflicts = ConflictRules.Evaluate(assignments, [emp], [wp], [], [], 2026, 47, closures);

        Assert.Contains(conflicts, c => c.Code == "SHIFT_CLOSED" && c.Severity == ConflictSeverity.Blocking);
    }
}
