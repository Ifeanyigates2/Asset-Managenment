using FrislEams.Web.Models;

namespace FrislEams.Web.Data;

public static class MongoHydrator
{
    public static void HydrateAssets(IEnumerable<Asset> assets, AppDbContext db)
    {
        var categories = db.AssetCategories.AsQueryable().ToDictionary(c => c.Id);
        var departments = db.Departments.AsQueryable().ToDictionary(d => d.Id);
        var locations = db.Locations.AsQueryable().ToDictionary(l => l.Id);
        var staff = db.Staff.AsQueryable().ToDictionary(s => s.Id);
        var suppliers = db.Suppliers.AsQueryable().ToDictionary(s => s.Id);

        foreach (var asset in assets)
        {
            if (asset.AssetCategoryId > 0)
            {
                asset.AssetCategory = categories.GetValueOrDefault(asset.AssetCategoryId);
            }

            if (asset.CurrentDepartmentId.HasValue)
            {
                asset.CurrentDepartment = departments.GetValueOrDefault(asset.CurrentDepartmentId.Value);
            }

            if (asset.CurrentLocationId.HasValue)
            {
                asset.CurrentLocation = locations.GetValueOrDefault(asset.CurrentLocationId.Value);
            }

            if (asset.CurrentCustodianId.HasValue)
            {
                asset.CurrentCustodian = staff.GetValueOrDefault(asset.CurrentCustodianId.Value);
            }

            if (asset.SupplierId.HasValue)
            {
                asset.Supplier = suppliers.GetValueOrDefault(asset.SupplierId.Value);
            }
        }
    }

    public static void HydrateStaff(IEnumerable<Staff> staffMembers, AppDbContext db)
    {
        var departments = db.Departments.AsQueryable().ToDictionary(d => d.Id);
        foreach (var staff in staffMembers)
        {
            staff.Department = departments.GetValueOrDefault(staff.DepartmentId);
        }
    }

    public static void HydrateAssignments(IEnumerable<AssetAssignment> assignments, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        var staff = db.Staff.AsQueryable().ToDictionary(s => s.Id);
        var departments = db.Departments.AsQueryable().ToDictionary(d => d.Id);
        var locations = db.Locations.AsQueryable().ToDictionary(l => l.Id);

        foreach (var assignment in assignments)
        {
            assignment.Asset = assets.GetValueOrDefault(assignment.AssetId);
            if (assignment.AssignedToStaffId.HasValue)
            {
                assignment.AssignedToStaff = staff.GetValueOrDefault(assignment.AssignedToStaffId.Value);
            }

            if (assignment.AssignedToDepartmentId.HasValue)
            {
                assignment.AssignedToDepartment = departments.GetValueOrDefault(assignment.AssignedToDepartmentId.Value);
            }

            assignment.AssignedLocation = locations.GetValueOrDefault(assignment.AssignedLocationId);
            if (assignment.ConfirmedByStaffId.HasValue)
            {
                assignment.ConfirmedByStaff = staff.GetValueOrDefault(assignment.ConfirmedByStaffId.Value);
            }
        }
    }

    public static void HydrateAssetRequests(IEnumerable<AssetRequest> requests, AppDbContext db)
    {
        var staff = db.Staff.AsQueryable().ToDictionary(s => s.Id);
        var departments = db.Departments.AsQueryable().ToDictionary(d => d.Id);
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);

        foreach (var request in requests)
        {
            request.RequestedByStaff = staff.GetValueOrDefault(request.RequestedByStaffId);
            request.Department = departments.GetValueOrDefault(request.DepartmentId);
            if (request.AssetId.HasValue)
            {
                request.Asset = assets.GetValueOrDefault(request.AssetId.Value);
            }
        }
    }

    public static void HydrateRepairRequests(IEnumerable<RepairRequest> requests, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        var staff = db.Staff.AsQueryable().ToDictionary(s => s.Id);
        var contractors = db.RepairContractors.AsQueryable().ToDictionary(c => c.Id);

        foreach (var request in requests)
        {
            request.Asset = assets.GetValueOrDefault(request.AssetId);
            request.ReportedByStaff = staff.GetValueOrDefault(request.ReportedByStaffId);
            if (request.AssignedContractorId.HasValue)
            {
                request.AssignedContractor = contractors.GetValueOrDefault(request.AssignedContractorId.Value);
            }
        }
    }

    public static void HydrateLoanRequests(IEnumerable<LoanRequest> requests, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        var staff = db.Staff.AsQueryable().ToDictionary(s => s.Id);

        foreach (var request in requests)
        {
            request.Asset = assets.GetValueOrDefault(request.AssetId);
            request.RequestedByStaff = staff.GetValueOrDefault(request.RequestedByStaffId);
        }
    }

    public static void HydrateRfidTags(IEnumerable<RfidTag> tags, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        foreach (var tag in tags)
        {
            if (tag.AssetId.HasValue)
                tag.Asset = assets.GetValueOrDefault(tag.AssetId.Value);
        }
    }

    public static void HydrateRfidEvents(IEnumerable<RfidEvent> events, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        foreach (var rfidEvent in events)
        {
            if (rfidEvent.AssetId.HasValue)
            {
                rfidEvent.Asset = assets.GetValueOrDefault(rfidEvent.AssetId.Value);
            }
        }
    }

    public static void HydrateAuditSessions(IEnumerable<AuditSession> sessions, AppDbContext db)
    {
        var departments = db.Departments.AsQueryable().ToDictionary(d => d.Id);
        foreach (var session in sessions)
        {
            if (session.DepartmentId.HasValue)
            {
                session.Department = departments.GetValueOrDefault(session.DepartmentId.Value);
            }
        }
    }

    public static void HydrateAuditResults(IEnumerable<AuditResult> results, AppDbContext db)
    {
        var assets = db.Assets.AsQueryable().ToDictionary(a => a.Id);
        foreach (var result in results)
        {
            result.Asset = assets.GetValueOrDefault(result.AssetId);
        }
    }
}
