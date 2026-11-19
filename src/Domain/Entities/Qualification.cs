using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Qualification : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Algemeen";
    public bool Active { get; set; } = true;

    public ICollection<EmployeeQualification> EmployeeQualifications { get; set; } = new List<EmployeeQualification>();
    public ICollection<WorkplaceRequirement> WorkplaceRequirements { get; set; } = new List<WorkplaceRequirement>();
}
