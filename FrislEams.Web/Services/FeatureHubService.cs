using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class MovementRecord
{
    public DateTime OccurredAt { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
}

public class ExceptionRecord
{
    public string Type { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public DateTime RaisedAt { get; set; }
    public string? ActionUrl { get; set; }
}

public class ReconciliationSummary
{
    public int TotalAssets { get; set; }
    public int Matched { get; set; }
    public int Unmatched { get; set; }
    public double CompliancePercent { get; set; }
    public int OpenAuditSessions { get; set; }
    public int RfidAlerts { get; set; }
}

public class FeatureHubService(AppDbContext db)
{
    public async Task<List<MovementRecord>> GetRecentMovementsAsync(int take = 25)
    {
        var records = new List<MovementRecord>();

        var histories = await db.AssetStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .Take(take)
            .ToListAsync();

        var assetIds = histories.Select(h => h.AssetId).Distinct().ToList();
        var assets = await db.Assets.AsQueryable().Where(a => assetIds.Contains(a.Id)).ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        var assetMap = assets.ToDictionary(a => a.Id);

        foreach (var history in histories)
        {
            assetMap.TryGetValue(history.AssetId, out var asset);
            records.Add(new MovementRecord
            {
                OccurredAt = history.ChangedAt,
                AssetTag = asset?.TagCode ?? $"#{history.AssetId}",
                AssetName = asset?.AssetName ?? "Unknown asset",
                MovementType = "Status Change",
                Detail = $"{history.PreviousStatus} → {history.NewStatus}",
                Actor = history.ChangedBy
            });
        }

        var assignments = await db.AssetAssignments
            .Include(a => a.Asset)
            .OrderByDescending(a => a.AssignedDate)
            .Take(take)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            records.Add(new MovementRecord
            {
                OccurredAt = assignment.AssignedDate,
                AssetTag = assignment.Asset?.TagCode ?? $"#{assignment.AssetId}",
                AssetName = assignment.Asset?.AssetName ?? "Unknown asset",
                MovementType = "Assignment",
                Detail = $"Assigned to staff #{assignment.AssignedToStaffId} ({assignment.Status})",
                Actor = assignment.AssignedBy
            });
        }

        return records.OrderByDescending(r => r.OccurredAt).Take(take).ToList();
    }

    public async Task<ReconciliationSummary> GetReconciliationSummaryAsync()
    {
        var total = await db.Assets.AsQueryable().CountAsync();
        var openAudits = await db.AuditSessions.AsQueryable().CountAsync(s => s.Status == "Open");
        var rfidAlerts = await db.RfidEvents.AsQueryable().CountAsync(e => e.AlertTriggered);
        var unmatched = await db.AuditResults.AsQueryable()
            .CountAsync(r => r.SeenStatus == "Missing" || r.SeenStatus == "Misplaced");
        var matched = Math.Max(0, total - unmatched);

        return new ReconciliationSummary
        {
            TotalAssets = total,
            Matched = matched,
            Unmatched = unmatched,
            CompliancePercent = total == 0 ? 100 : Math.Round(matched * 100.0 / total, 1),
            OpenAuditSessions = openAudits,
            RfidAlerts = rfidAlerts
        };
    }

    public async Task<List<ExceptionRecord>> GetExceptionsAsync()
    {
        var exceptions = new List<ExceptionRecord>();

        var alerts = await db.RfidEvents.AsQueryable()
            .Where(e => e.AlertTriggered)
            .OrderByDescending(e => e.EventTime)
            .Take(20)
            .ToListAsync();

        foreach (var alert in alerts)
        {
            exceptions.Add(new ExceptionRecord
            {
                Type = "RFID Alert",
                AssetTag = alert.RfidCode,
                Description = alert.AlertMessage ?? alert.EventType,
                Severity = "High",
                RaisedAt = alert.EventTime,
                ActionUrl = "/Workflow/Rfid"
            });
        }

        var variances = await db.AuditResults.AsQueryable()
            .Include(r => r.Asset)
            .Where(r => r.SeenStatus == "Missing" || r.SeenStatus == "Misplaced")
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        foreach (var variance in variances)
        {
            exceptions.Add(new ExceptionRecord
            {
                Type = "Audit Variance",
                AssetTag = variance.Asset?.TagCode ?? $"Asset #{variance.AssetId}",
                Description = $"{variance.SeenStatus}: {variance.Notes ?? variance.Variance ?? "Reconciliation mismatch"}",
                Severity = "Medium",
                RaisedAt = variance.CreatedAt,
                ActionUrl = $"/Audit/Variance/{variance.AuditSessionId}"
            });
        }

        var pendingRepairs = await db.RepairRequests.AsQueryable()
            .Where(r => r.Status == "Pending Admin Review")
            .Take(10)
            .ToListAsync();

        foreach (var repair in pendingRepairs)
        {
            exceptions.Add(new ExceptionRecord
            {
                Type = "Pending Repair",
                AssetTag = $"Asset #{repair.AssetId}",
                Description = repair.Description ?? "Repair awaiting review",
                Severity = "Warning",
                RaisedAt = repair.CreatedAt,
                ActionUrl = "/Workflow/Repairs"
            });
        }

        return exceptions.OrderByDescending(e => e.RaisedAt).Take(30).ToList();
    }

    public async Task<List<Asset>> GetDisposedAssetsAsync()
    {
        var disposedStatuses = new[]
        {
            AssetStatus.Retired,
            AssetStatus.Sold,
            AssetStatus.Discarded,
            AssetStatus.PendingReplacement
        };
        var assets = await db.Assets.AsQueryable()
            .Where(a => disposedStatuses.Contains(a.CurrentStatus))
            .OrderByDescending(a => a.UpdatedAt)
            .Take(50)
            .ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        return assets;
    }

    public async Task<List<Asset>> GetStaffAssetsAsync(string? username, string? displayName)
    {
        var staff = await db.Staff.AsQueryable()
            .FirstOrDefaultAsync(s =>
                s.FullName.Equals(displayName ?? "", StringComparison.OrdinalIgnoreCase)
                || s.Email.StartsWith(username ?? "", StringComparison.OrdinalIgnoreCase));

        var assets = await db.Assets.AsQueryable().ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);

        if (staff is null)
        {
            return assets
                .Where(a => a.CurrentStatus == AssetStatus.ActiveAssigned)
                .Take(8)
                .ToList();
        }

        return assets
            .Where(a => a.CurrentCustodianId == staff.Id)
            .OrderByDescending(a => a.UpdatedAt)
            .ToList();
    }

    public async Task<List<Notification>> GetNotificationsForRoleAsync(string? role, bool unreadOnly = false)
    {
        var portal = PortalService.GetPortalForRole(role);
        var targetRoles = portal switch
        {
            PortalService.AdminPortal => new[] { "Admin", "Backoffice" },
            PortalService.AuditorPortal => new[] { "Auditor", "Backoffice", "Admin" },
            PortalService.StaffPortal => new[] { "Staff", "DepartmentHead", "Backoffice" },
            _ => new[] { "Backoffice", "Admin" }
        };

        var query = db.Notifications.AsQueryable()
            .Where(n => targetRoles.Contains(n.TargetRole));

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
    }
}
