using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class Department : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public ICollection<Workplace> Workplaces { get; set; } = new List<Workplace>();
}
