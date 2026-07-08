using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class AssignmentsController(AppDbContext db, AssetLifecycleService lifecycleService, RfidTagService rfidTagService) : Controller
{
    private const string StaffReturnUrl = "/Portal/Staff";

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var assignments = await db.AssetAssignments
            .AsQueryable()
            .OrderByDescending(a => a.AssignedDate)
            .ToListAsync();
        MongoHydrator.HydrateAssignments(assignments, db);
        ViewBag.Staff = await GetActiveStaffAsync();
        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assetId)
    {
        ViewBag.Assets = await db.Assets.AsQueryable().Where(a => a.CurrentStatus == AssetStatus.RegisteredUnassigned).ToListAsync();
        ViewBag.Staff = await GetActiveStaffAsync();
        ViewBag.Departments = await db.Departments.ToListAsync();
        ViewBag.Locations = await db.Locations.ToListAsync();

        return View(new AssignmentInitiateVm { AssetId = assetId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentInitiateVm vm)
    {
        if (!ModelState.IsValid)
        {
            return await Create(vm.AssetId);
        }

        var asset = await db.Assets.FindAsync(vm.AssetId);
        if (asset is null)
        {
            return NotFound();
        }

        if (!lifecycleService.ChangeStatus(asset, AssetStatus.AssignedPendingConfirmation, "Assignment initiated", "Admin"))
        {
            TempData["Error"] = "Asset is not in an assignable state.";
            return RedirectToAction("Index", "Assets");
        }

        db.AssetAssignments.Add(new AssetAssignment
        {
            AssetId = vm.AssetId,
            AssignedToStaffId = vm.AssignedToStaffId,
            AssignedToDepartmentId = vm.AssignedToDepartmentId,
            AssignedLocationId = vm.AssignedLocationId,
            AssignedCondition = vm.AssignedCondition,
            ExpectedReturnDate = vm.ExpectedReturnDate,
            Notes = vm.Notes,
            Status = "Pending"
        });

        asset.CurrentCondition = vm.AssignedCondition;
        asset.CurrentCustodianId = vm.AssignedToStaffId;
        asset.CurrentDepartmentId = vm.AssignedToDepartmentId;
        asset.CurrentLocationId = vm.AssignedLocationId;

        await db.SaveChangesAsync();
        TempData["Success"] = "Assignment initiated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(AssignmentConfirmVm vm, string? returnUrl)
    {
        IActionResult RedirectBack()
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        var assignment = await db.AssetAssignments.AsQueryable()
            .FirstOrDefaultAsync(a => a.Id == vm.AssignmentId);
        if (assignment is not null)
        {
            MongoHydrator.HydrateAssignments([assignment], db);
        }

        if (assignment?.Asset is null)
        {
            return NotFound();
        }

        var role = HttpContext.Session.GetString("UserRole");
        var portal = PortalService.GetPortalForRole(role);
        if (string.Equals(portal, PortalService.StaffPortal, StringComparison.OrdinalIgnoreCase))
        {
            var currentStaff = await GetCurrentStaffAsync();

            if (currentStaff is null
                || assignment.AssignedToStaffId != currentStaff.Id
                || vm.ConfirmedByStaffId != currentStaff.Id)
            {
                TempData["Error"] = "You can only confirm receipt for assets assigned to you.";
                return RedirectBack();
            }
        }

        if (!string.Equals(assignment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "This assignment is not awaiting confirmation.";
            return RedirectBack();
        }

        if (!string.IsNullOrWhiteSpace(vm.ScannedRfidCode))
        {
            var tag = await rfidTagService.FindByAssetIdAsync(assignment.AssetId);
            if (tag is null || !string.Equals(tag.RfidCode, vm.ScannedRfidCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "RFID scan does not match this asset. Verify the tag before confirming.";
                return RedirectBack();
            }
        }

        assignment.ConfirmationDate = DateTime.UtcNow;
        assignment.ConfirmedByStaffId = vm.ConfirmedByStaffId;
        assignment.ConfirmedCondition = vm.ConfirmedCondition;
        assignment.Status = "Confirmed";

        assignment.Asset.CurrentCondition = vm.ConfirmedCondition;

        var ok = lifecycleService.ChangeStatus(
            assignment.Asset,
            AssetStatus.ActiveAssigned,
            "Assignee confirmed receipt",
            $"Staff:{vm.ConfirmedByStaffId}");

        if (!ok)
        {
            TempData["Error"] = "Confirmation failed due to invalid state transition.";
            return RedirectBack();
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"You have accepted {assignment.Asset.AssetName} ({assignment.Asset.TagCode}).";
        return RedirectBack();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(AssignmentRejectVm vm, string? returnUrl)
    {
        IActionResult RedirectBack()
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return Redirect(returnUrl);
            }

            return Redirect(StaffReturnUrl);
        }

        var role = HttpContext.Session.GetString("UserRole");
        var portal = PortalService.GetPortalForRole(role);
        if (!string.Equals(portal, PortalService.StaffPortal, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var assignment = await db.AssetAssignments.AsQueryable()
            .FirstOrDefaultAsync(a => a.Id == vm.AssignmentId);

        if (assignment is not null)
        {
            MongoHydrator.HydrateAssignments([assignment], db);
        }

        if (assignment?.Asset is null)
        {
            return NotFound();
        }

        var currentStaff = await GetCurrentStaffAsync();
        if (currentStaff is null
            || assignment.AssignedToStaffId != currentStaff.Id
            || vm.RejectedByStaffId != currentStaff.Id)
        {
            TempData["Error"] = "You can only reject assignments addressed to you.";
            return RedirectBack();
        }

        if (!string.Equals(assignment.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "This assignment is not awaiting confirmation.";
            return RedirectBack();
        }

        var reason = string.IsNullOrWhiteSpace(vm.RejectionReason)
            ? "Assignee declined receipt"
            : vm.RejectionReason.Trim();

        assignment.Status = "Rejected";
        assignment.RejectedByStaffId = vm.RejectedByStaffId;
        assignment.RejectionReason = reason;
        assignment.ConfirmationDate = DateTime.UtcNow;

        var asset = assignment.Asset;
        asset.CurrentCustodianId = null;

        var storeId = db.Locations
            .AsQueryable()
            .Where(l => l.Code == "STORE")
            .Select(l => (int?)l.Id)
            .FirstOrDefault();
        if (storeId.HasValue)
        {
            asset.CurrentLocationId = storeId;
        }

        var ok = lifecycleService.ChangeStatus(
            asset,
            AssetStatus.RegisteredUnassigned,
            $"Assignee rejected receipt: {reason}",
            $"Staff:{vm.RejectedByStaffId}");

        if (!ok)
        {
            TempData["Error"] = "Rejection failed due to invalid state transition.";
            return RedirectBack();
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Assignment for {asset.AssetName} ({asset.TagCode}) was rejected.";
        return RedirectBack();
    }

    private async Task<List<Staff>> GetActiveStaffAsync()
    {
        var staff = await db.Staff
            .AsQueryable()
            .Where(s => s.IsActive)
            .OrderBy(s => s.FullName)
            .ToListAsync();
        MongoHydrator.HydrateStaff(staff, db);
        return staff;
    }

    private async Task<Staff?> GetCurrentStaffAsync()
    {
        var username = HttpContext.Session.GetString("LoginUsername");
        var displayName = HttpContext.Session.GetString("UserName");
        return await StaffRepository.FindBySessionAsync(db, username, displayName);
    }
}
