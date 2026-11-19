using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class AuditEntry : IEntity
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
}
