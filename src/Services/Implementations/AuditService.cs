using Domain.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Services.Implementations;

public class AuditService(RepositoryContext db) : IAuditService
{
    public async Task LogAsync(string userName, string entityType, int? entityId, string action, string? oldValue, string? newValue, string? reason = null)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            At = DateTime.UtcNow,
            UserName = userName,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason
        });
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int take = 100) =>
        await db.AuditEntries.OrderByDescending(a => a.At).Take(take).AsNoTracking().ToListAsync();
}
