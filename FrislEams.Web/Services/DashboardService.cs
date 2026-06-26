using FrislEams.Web.Data;
using FrislEams.Web.Domain;

namespace FrislEams.Web.Services;

public class AdminDashboardMetrics
{
    public int TotalAssets { get; set; }
    public int ActiveAssigned { get; set; }
    public int RegisteredUnassigned { get; set; }
    public int UnderRepair { get; set; }
    public int LoanedOut { get; set; }
    public int Damaged { get; set; }
    public int Retired { get; set; }
    public int TotalValue { get; set; }
    public int PendingAssignmentConfirmations { get; set; }
    public int PendingRepairRequests { get; set; }
    public int PendingAssetRequests { get; set; }
    public int PendingLoanRequests { get; set; }
    public int ExitAlerts { get; set; }
    public int UnreadNotifications { get; set; }
    public int WarrantyExpiringSoon { get; set; }
    public int TotalStaff { get; set; }
    public int TotalDepartments { get; set; }
    public List<FinancialUpdatePromptVm> AssetsMissingFinancialDetails { get; set; } = [];
    public Dictionary<string, int> AssetsByCategory { get; set; } = new();
    public Dictionary<string, int> AssetsByStatus { get; set; } = new();
    public Dictionary<string, int> AssetsByDepartment { get; set; } = new();
}

public class FinancialUpdatePromptVm
{
    public int AssetId { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public bool MissingPurchaseCost { get; set; }
    public bool MissingGlCode { get; set; }
}

public class BackofficeManualMetrics
{
    public int TotalAssets { get; set; }
    public int AssetsAssigned { get; set; }
    public int AssetsAwaitingRfid { get; set; }
    public int AssetsWithoutCustodian { get; set; }
    public int PendingTransfers { get; set; }
}

public class DashboardService(AppDbContext db, FeatureHubService hub)
{
    public async Task<AdminDashboardMetrics> GetAdminMetricsFullAsync()
    {
        var threeMonths = DateTime.UtcNow.AddMonths(3);
        var assets = await db.Assets.AsQueryable().ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);

        var metrics = new AdminDashboardMetrics
        {
            TotalAssets = assets.Count,
            ActiveAssigned = assets.Count(a => a.CurrentStatus == AssetStatus.ActiveAssigned),
            RegisteredUnassigned = assets.Count(a => a.CurrentStatus == AssetStatus.RegisteredUnassigned),
            UnderRepair = assets.Count(a => a.CurrentStatus == AssetStatus.UnderRepair),
            LoanedOut = assets.Count(a => a.CurrentStatus is AssetStatus.LoanedOutInternal or AssetStatus.LoanedOutExternal),
            Damaged = assets.Count(a => a.CurrentStatus == AssetStatus.Damaged),
            Retired = assets.Count(a => a.CurrentStatus is AssetStatus.Retired or AssetStatus.Sold or AssetStatus.Discarded),
            PendingAssignmentConfirmations = assets.Count(a => a.CurrentStatus == AssetStatus.AssignedPendingConfirmation),
            WarrantyExpiringSoon = assets.Count(a => a.WarrantyExpiryDate.HasValue && a.WarrantyExpiryDate.Value <= threeMonths && a.WarrantyExpiryDate.Value >= DateTime.UtcNow),
            PendingRepairRequests = await db.RepairRequests.AsQueryable().CountAsync(r => r.Status == "Pending Admin Review"),
            PendingAssetRequests = await db.AssetRequests.AsQueryable().CountAsync(r => r.Status.StartsWith("Pending")),
            PendingLoanRequests = await db.LoanRequests.AsQueryable().CountAsync(l => l.Status == "Pending"),
            ExitAlerts = await db.RfidEvents.AsQueryable().CountAsync(e => e.AlertTriggered),
            UnreadNotifications = await db.Notifications.AsQueryable().CountAsync(n => !n.IsRead && n.TargetRole == "Backoffice"),
            TotalStaff = await db.Staff.AsQueryable().CountAsync(s => s.IsActive),
            TotalDepartments = await db.Departments.AsQueryable().CountAsync(),
            AssetsMissingFinancialDetails = assets
                .Where(a => !a.PurchaseCost.HasValue || string.IsNullOrWhiteSpace(a.GlCode))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new FinancialUpdatePromptVm
                {
                    AssetId = a.Id,
                    TagCode = a.TagCode,
                    AssetName = a.AssetName,
                    MissingPurchaseCost = !a.PurchaseCost.HasValue,
                    MissingGlCode = string.IsNullOrWhiteSpace(a.GlCode)
                })
                .ToList(),
            AssetsByCategory = assets
                .Where(a => a.AssetCategory != null)
                .GroupBy(a => a.AssetCategory!.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssetsByDepartment = assets
                .Where(a => a.CurrentDepartment != null)
                .GroupBy(a => a.CurrentDepartment!.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            AssetsByStatus = assets
                .GroupBy(a => a.CurrentStatus.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        if (assets.Any(a => a.PurchaseCost.HasValue))
            metrics.TotalValue = (int)(assets.Where(a => a.PurchaseCost.HasValue).Sum(a => a.PurchaseCost!.Value) / 1_000_000);

        return metrics;
    }

    public async Task<BackofficeManualMetrics> GetBackofficeManualMetricsAsync()
    {
        var assets = await db.Assets.AsQueryable().ToListAsync();
        var taggedAssetIds = await db.RfidTags.AsQueryable()
            .Where(t => t.IsActive && t.AssetId.HasValue)
            .Select(t => t.AssetId!.Value)
            .ToListAsync();

        return new BackofficeManualMetrics
        {
            TotalAssets = assets.Count,
            AssetsAssigned = assets.Count(a => a.CurrentStatus == AssetStatus.ActiveAssigned),
            AssetsAwaitingRfid = assets.Count(a => !taggedAssetIds.Contains(a.Id)),
            AssetsWithoutCustodian = assets.Count(a => !a.CurrentCustodianId.HasValue && a.CurrentStatus != AssetStatus.Retired),
            PendingTransfers = await db.AssetTransfers.AsQueryable().CountAsync(t => t.Status.StartsWith("Pending") || t.Status == "Approved" || t.Status == "Departed")
        };
    }

    public async Task<AdminDashboardMetrics> GetBackofficeMetricsAsync() => await GetAdminMetricsFullAsync();

    public async Task<Dictionary<string, int>> GetAdminPortalMetricsAsync()
    {
        var users = await db.UserAccounts.AsQueryable().ToListAsync();
        var rfidDevices = await db.RfidTags.AsQueryable().CountAsync();
        var roles = users.Select(u => u.Role).Distinct().Count();

        return new Dictionary<string, int>
        {
            ["Total Users"] = users.Count,
            ["Active Users"] = users.Count(u => u.IsActive),
            ["Roles"] = roles,
            ["RFID Devices"] = rfidDevices
        };
    }

    public async Task<Dictionary<string, object>> GetAuditorPortalMetricsAsync()
    {
        var total = await db.Assets.AsQueryable().CountAsync();
        var unmatched = await db.AuditResults.AsQueryable()
            .CountAsync(r => r.SeenStatus == "Missing" || r.SeenStatus == "Misplaced");
        var matched = Math.Max(0, total - unmatched);
        var exceptions = await db.RfidEvents.AsQueryable().CountAsync(e => e.AlertTriggered) + unmatched;

        return new Dictionary<string, object>
        {
            ["TotalAssetsAudited"] = total,
            ["Reconciled"] = matched,
            ["Exceptions"] = exceptions,
            ["CompliancePercent"] = total == 0 ? 100.0 : Math.Round(matched * 100.0 / total, 1)
        };
    }

    public async Task<Dictionary<string, int>> GetStaffPortalMetricsAsync(string? username, string? displayName)
    {
        var myAssets = await hub.GetStaffAssetsAsync(username, displayName);
        var notifications = await hub.GetNotificationsForRoleAsync(RoleName.Staff, unreadOnly: true);

        return new Dictionary<string, int>
        {
            ["MyAssets"] = myAssets.Count,
            ["PendingRequests"] = await db.AssetRequests.AsQueryable().CountAsync(r => r.Status.StartsWith("Pending")),
            ["DueForReturn"] = await db.LoanRequests.AsQueryable().CountAsync(l => l.Status == "Approved"),
            ["Notifications"] = notifications.Count
        };
    }

    public async Task<Dictionary<string, int>> GetAdminMetricsAsync()
    {
        var m = await GetAdminMetricsFullAsync();
        return new Dictionary<string, int>
        {
            ["Total Assets"] = m.TotalAssets,
            ["Active – Assigned"] = m.ActiveAssigned,
            ["Registered – Unassigned"] = m.RegisteredUnassigned,
            ["Under Repair"] = m.UnderRepair,
            ["Loaned Out"] = m.LoanedOut,
            ["Damaged"] = m.Damaged,
            ["Pending Confirmations"] = m.PendingAssignmentConfirmations,
            ["Pending Repairs"] = m.PendingRepairRequests,
            ["Pending Requests"] = m.PendingAssetRequests,
            ["RFID Alerts"] = m.ExitAlerts,
            ["Warranty Expiring Soon"] = m.WarrantyExpiringSoon
        };
    }
}
