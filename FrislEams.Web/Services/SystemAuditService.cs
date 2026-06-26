using FrislEams.Web.Data;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class SystemAuditService(AppDbContext db)
{
    public async Task LogAsync(string username, string action, string entityType, string? entityId = null, string? previousValue = null, string? newValue = null)
    {
        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PreviousValue = previousValue,
            NewValue = newValue
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<SystemAuditLog>> GetRecentAsync(int take = 100)
        => await db.SystemAuditLogs.AsQueryable().OrderByDescending(l => l.OccurredAt).Take(take).ToListAsync();
}
