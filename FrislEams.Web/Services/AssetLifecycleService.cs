using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class AssetLifecycleService(AppDbContext db)
{
    private static readonly HashSet<AssetStatus> TerminalStates =
    [AssetStatus.Retired, AssetStatus.Sold, AssetStatus.Discarded];

    private static readonly Dictionary<AssetStatus, HashSet<AssetStatus>> Transitions = new()
    {
        [AssetStatus.UnregisteredUnassigned] = [AssetStatus.RegisteredUnassigned],
        [AssetStatus.RegisteredUnassigned] =
        [
            AssetStatus.AssignedPendingConfirmation,
            AssetStatus.Retired, AssetStatus.Sold, AssetStatus.Discarded
        ],
        [AssetStatus.AssignedPendingConfirmation] = [AssetStatus.ActiveAssigned, AssetStatus.RegisteredUnassigned],
        [AssetStatus.ActiveAssigned] =
        [
            AssetStatus.RecalledToStorage, AssetStatus.UnderRepair, AssetStatus.Damaged,
            AssetStatus.LoanedOutInternal, AssetStatus.LoanedOutExternal,
            AssetStatus.Retired, AssetStatus.Sold, AssetStatus.Discarded
        ],
        [AssetStatus.RecalledToStorage] = [AssetStatus.RegisteredUnassigned, AssetStatus.AssignedPendingConfirmation],
        [AssetStatus.Damaged] = [AssetStatus.UnderRepair, AssetStatus.Retired, AssetStatus.Sold, AssetStatus.Discarded],
        [AssetStatus.UnderRepair] = [AssetStatus.ActiveAssigned, AssetStatus.PendingReplacement, AssetStatus.Discarded],
        [AssetStatus.PendingReplacement] = [AssetStatus.Retired, AssetStatus.Discarded],
        [AssetStatus.LoanedOutInternal] = [AssetStatus.ActiveAssigned],
        [AssetStatus.LoanedOutExternal] = [AssetStatus.ActiveAssigned],
        [AssetStatus.UnderProcurement] = [AssetStatus.UnregisteredUnassigned]
    };

    public (bool IsValid, string Error) ValidateTransition(AssetStatus from, AssetStatus to)
    {
        if (TerminalStates.Contains(from))
            return (false, "Asset is in a terminal state and cannot be transitioned.");
        if (!Transitions.TryGetValue(from, out var validStates) || !validStates.Contains(to))
            return (false, $"Invalid transition: {from} → {to}");
        return (true, string.Empty);
    }

    public bool ChangeStatus(Asset asset, AssetStatus nextStatus, string reason, string changedBy)
    {
        if (string.IsNullOrWhiteSpace(reason)) return false;

        var validation = ValidateTransition(asset.CurrentStatus, nextStatus);
        if (!validation.IsValid) return false;

        var previous = asset.CurrentStatus;
        asset.CurrentStatus = nextStatus;
        asset.UpdatedAt = DateTime.UtcNow;

        // Auto-move to storage for certain statuses
        if (nextStatus is AssetStatus.PendingReplacement or AssetStatus.RecalledToStorage or AssetStatus.Damaged)
        {
            var storeId = db.Locations
            .AsQueryable()
            .Where(l => l.Code == "STORE")
            .Select(l => (int?)l.Id)
            .FirstOrDefault();
            if (storeId.HasValue)
                asset.CurrentLocationId = storeId;
        }

        db.AssetStatusHistories.Add(new AssetStatusHistory
        {
            AssetId = asset.Id,
            PreviousStatus = previous,
            NewStatus = nextStatus,
            ChangedBy = changedBy,
            Reason = reason,
            LocationSnapshotId = asset.CurrentLocationId,
            DepartmentSnapshotId = asset.CurrentDepartmentId,
            CustodianSnapshotId = asset.CurrentCustodianId,
            ChangedAt = DateTime.UtcNow
        });

        db.Assets.Update(asset);
        return true;
    }
}
