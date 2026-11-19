using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class WeekPlan : IEntity
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public WeekPlanStatus Status { get; set; } = WeekPlanStatus.Concept;
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public int Version { get; set; } = 1;

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}

public class ShiftAssignment : IEntity
{
    public int Id { get; set; }
    public int WeekPlanId { get; set; }
    public WeekPlan? WeekPlan { get; set; }
    public DateOnly Date { get; set; }
    public int WorkplaceId { get; set; }
    public Workplace? Workplace { get; set; }
    public string ShiftName { get; set; } = "Ochtend";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal PlannedHours { get; set; } = 8;
    public string? OverrideReason { get; set; }
}

public class WeekPlanSnapshot : IEntity
{
    public int Id { get; set; }
    public int WeekPlanId { get; set; }
    public WeekPlan? WeekPlan { get; set; }
    public int Version { get; set; }
    public WeekPlanStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
