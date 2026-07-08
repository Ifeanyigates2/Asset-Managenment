using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Data;

public static class AssignmentRepository
{
    /// <summary>
    /// Ensures Emmanuel (FR-009) has a pending receipt assignment for the Ubiquiti AP demo asset.
    /// Safe to run on every startup — skips if a pending assignment already exists for that staff/asset pair.
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

        var existingPending = await db.AssetAssignments.AsQueryable()
            .FirstOrDefaultAsync(a =>
                a.AssetId == asset.Id
                && a.AssignedToStaffId == staff.Id
                && a.Status == "Pending",
                cancellationToken);

        if (existingPending is not null)
        {
            if (asset.CurrentStatus != AssetStatus.AssignedPendingConfirmation)
            {
                asset.CurrentStatus = AssetStatus.AssignedPendingConfirmation;
                asset.CurrentCustodianId = staff.Id;
                asset.UpdatedAt = DateTime.UtcNow;
                db.Assets.Update(asset);
                await db.SaveChangesAsync(cancellationToken);
            }

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

        // Only create if the asset is unassigned or already in pending-confirmation for this staff.
        if (asset.CurrentStatus is not (AssetStatus.RegisteredUnassigned or AssetStatus.AssignedPendingConfirmation))
        {
            return;
        }

        db.AssetAssignments.Add(new AssetAssignment
        {
            AssetId = asset.Id,
            AssignedToStaffId = staff.Id,
            AssignedToDepartmentId = itDept?.Id ?? staff.DepartmentId,
            AssignedLocationId = hqLoc.Id,
            AssignedCondition = asset.CurrentCondition,
            Status = "Pending",
            AssignedBy = "Admin",
            AssignedDate = DateTime.UtcNow,
            Notes = "Issued to IT — awaiting receipt confirmation"
        });

        asset.CurrentStatus = AssetStatus.AssignedPendingConfirmation;
        asset.CurrentCustodianId = staff.Id;
        asset.CurrentDepartmentId = itDept?.Id ?? staff.DepartmentId;
        asset.CurrentLocationId = hqLoc.Id;
        asset.UpdatedAt = DateTime.UtcNow;
        db.Assets.Update(asset);

        await db.SaveChangesAsync(cancellationToken);
        Console.WriteLine(
            $"FRISL EAMS startup: ensured pending receipt for '{staff.FullName}' on asset '{assetTag}'.");
    }
}
