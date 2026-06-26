using FrislEams.Web.Data;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class AuditTrailsController(AppDbContext db, RoleGuard roleGuard, SystemAuditService auditService) : Controller
{
    [HttpGet("/AuditTrails")]
    public async Task<IActionResult> Index()
    {
        if (!PortalCapabilities.CanViewAuditLogs(GetPortal()))
        {
            return Forbid();
        }

        ViewData["Title"] = "Audit Trail";
        var trails = await db.AssetStatusHistories
            .OrderByDescending(h => h.ChangedAt)
            .Take(100)
            .ToListAsync();

        var assetIds = trails.Select(t => t.AssetId).Distinct().ToList();
        var assets = await db.Assets.AsQueryable().Where(a => assetIds.Contains(a.Id)).ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        ViewBag.Assets = assets.ToDictionary(a => a.Id);
        ViewBag.SystemLogs = await auditService.GetRecentAsync(100);

        return View(trails);
    }

    private string GetPortal()
        => PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
}
