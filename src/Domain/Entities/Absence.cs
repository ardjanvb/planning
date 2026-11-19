using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Absence : IEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public AbsenceType Type { get; set; }
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Requested;
    public string? Remark { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
