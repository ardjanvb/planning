using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Workplace : IEntity
{
    public int Id { get; set; }
    public string ExternalNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
    public int MinimumStaffing { get; set; }
    public int RequiredBhvCount { get; set; }
    public string? Remarks { get; set; }

    public ICollection<WorkplaceRequirement> Requirements { get; set; } = new List<WorkplaceRequirement>();
    public ICollection<WorkplaceCalendarException> CalendarExceptions { get; set; } = new List<WorkplaceCalendarException>();
    public ICollection<CapacityNeed> CapacityNeeds { get; set; } = new List<CapacityNeed>();
    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}

public class WorkplaceRequirement : IEntity
{
    public int Id { get; set; }
    public int WorkplaceId { get; set; }
    public Workplace? Workplace { get; set; }
    public int? JobFunctionId { get; set; }
    public JobFunction? JobFunction { get; set; }
    public int? QualificationId { get; set; }
    public Qualification? Qualification { get; set; }
    public int RequiredCount { get; set; } = 1;
}
