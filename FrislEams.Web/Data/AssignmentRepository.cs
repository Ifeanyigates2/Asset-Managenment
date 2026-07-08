using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Data;

public static class AssignmentRepository
{
    /// <summary>
    /// Ensures Emmanuel (FR-009) has a pending receipt assignment for the Ubiquiti AP demo asset.
    /// Safe to run on every startup — resets the demo asset to AssignedPendingConfirmation so Staff UI always shows Receive.
    /// </summary>
    public static async Task EnsureDemoPendingReceiptAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        const string assetTag = "FRISL-2024-NET-IT-001";
        const string staffId = "FR-009";

        var staff = await db.Staff.AsQueryable()
            .FirstOrDefaultAsync(s => s.StaffId == staffId, cancellationToken);
        var asset = await db.Assets.AsQueryable()
            .FirstOrDefaultAsync(a => a.TagCode == assetTag, cancellationToken);

        if (staff is null || asset is null)
        {
            Console.WriteLine(
                $"FRISL EAMS startup: skipped demo pending receipt — staff '{staffId}' or asset '{assetTag}' not found.");
            return;
        }

        var itDept = await db.Departments.AsQueryable()
            .FirstOrDefaultAsync(d => d.Code == "IT", cancellationToken);
        var hqLoc = await db.Locations.AsQueryable()
            .FirstOrDefaultAsync(l => l.Code == "HQ-ABJ", cancellationToken)
            ?? await db.Locations.AsQueryable().FirstOrDefaultAsync(cancellationToken);

        if (hqLoc is null)
        {
            return;
        }

        var existingPending = await db.AssetAssignments.AsQueryable()
            .FirstOrDefaultAsync(a =>
                a.AssetId == asset.Id
                && a.AssignedToStaffId == staff.Id
                && a.Status == "Pending",
                cancellationToken);

        if (existingPending is null)
        {
            // Re-open a prior Confirmed assignment for this demo pair, or create a new Pending one.
            var priorForStaff = await db.AssetAssignments.AsQueryable()
                .Where(a => a.AssetId == asset.Id && a.AssignedToStaffId == staff.Id)
                .OrderByDescending(a => a.AssignedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (priorForStaff is not null)
            {
                priorForStaff.Status = "Pending";
                priorForStaff.ConfirmedCondition = null;
                priorForStaff.ConfirmationDate = null;
                priorForStaff.AssignedCondition = asset.CurrentCondition ?? "Good";
                priorForStaff.AssignedDate = DateTime.UtcNow;
                priorForStaff.Notes = "Issued to IT — awaiting receipt confirmation";
                priorForStaff.AssignedBy = "Admin";
                db.AssetAssignments.Update(priorForStaff);
                existingPending = priorForStaff;
            }
            else
            {
                existingPending = new AssetAssignment
                {
                    AssetId = asset.Id,
                    AssignedToStaffId = staff.Id,
                    AssignedToDepartmentId = itDept?.Id ?? staff.DepartmentId,
                    AssignedLocationId = hqLoc.Id,
                    AssignedCondition = asset.CurrentCondition ?? "Good",
                    Status = "Pending",
                    AssignedBy = "Admin",
                    AssignedDate = DateTime.UtcNow,
                    Notes = "Issued to IT — awaiting receipt confirmation"
                };
                db.AssetAssignments.Add(existingPending);
            }
        }

        // Always force the demo asset into the pending-confirmation state for Emmanuel.
        asset.CurrentStatus = AssetStatus.AssignedPendingConfirmation;
        asset.CurrentCustodianId = staff.Id;
        asset.CurrentDepartmentId = itDept?.Id ?? staff.DepartmentId;
        asset.CurrentLocationId = hqLoc.Id;
        asset.UpdatedAt = DateTime.UtcNow;
        db.Assets.Update(asset);

        await db.SaveChangesAsync(cancellationToken);
        Console.WriteLine(
            $"FRISL EAMS startup: ensured pending receipt for '{staff.FullName}' on asset '{assetTag}' (status={asset.CurrentStatus}).");
    }
}
