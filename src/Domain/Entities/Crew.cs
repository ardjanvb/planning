using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Crew : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CrewPatternId { get; set; }
    public CrewPattern? CrewPattern { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<EmployeeCrewMembership> Memberships { get; set; } = new List<EmployeeCrewMembership>();
}
