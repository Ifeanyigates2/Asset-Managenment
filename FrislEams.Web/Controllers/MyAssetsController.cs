using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class MyAssetsController(AppDbContext db, FeatureHubService hub) : Controller
{
    [HttpGet("/MyAssets")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "My Assets";
        var username = HttpContext.Session.GetString("LoginUsername");
        var displayName = HttpContext.Session.GetString("UserName");
        return View(await hub.GetStaffAssetsAsync(username, displayName));
    }

    [HttpGet("/MyAssets/ConfirmReceipt")]
    public async Task<IActionResult> ConfirmReceipt(int assetId)
    {
        ViewData["Title"] = "Receive Asset";

        var role = HttpContext.Session.GetString("UserRole");
        var portal = PortalService.GetPortalForRole(role);
        if (!string.Equals(portal, PortalService.StaffPortal, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var username = HttpContext.Session.GetString("LoginUsername");
        var displayName = HttpContext.Session.GetString("UserName");
        var staff = await StaffRepository.FindBySessionAsync(db, username, displayName);

        if (staff is null)
        {
            TempData["Error"] = "Your staff profile could not be found. Contact IT support.";
            return Redirect("/Portal/Staff");
        }

        var assignment = await db.AssetAssignments.AsQueryable()
            .Include(a => a.Asset)
            .FirstOrDefaultAsync(a =>
                a.AssetId == assetId
                && a.AssignedToStaffId == staff.Id
                && a.Status == "Pending");

        if (assignment?.Asset is null || assignment.Asset.CurrentStatus != AssetStatus.AssignedPendingConfirmation)
        {
            TempData["Error"] = "This asset is not awaiting confirmation for you.";
            return Redirect("/Portal/Staff");
        }

        ViewBag.Asset = assignment.Asset;
        ViewBag.Assignment = assignment;

        return View(new AssignmentConfirmVm
        {
            AssignmentId = assignment.Id,
            ConfirmedByStaffId = staff.Id,
            ConfirmedCondition = assignment.Asset.CurrentCondition
        });
    }
}
