namespace Services.Contracts;

public interface IAuditService
{
    Task LogAsync(string userName, string entityType, int? entityId, string action, string? oldValue, string? newValue, string? reason = null);
    Task<IReadOnlyList<Domain.Entities.AuditEntry>> GetRecentAsync(int take = 100);
}
