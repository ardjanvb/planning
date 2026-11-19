using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class JobFunction : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<WorkplaceRequirement> WorkplaceRequirements { get; set; } = new List<WorkplaceRequirement>();
}
