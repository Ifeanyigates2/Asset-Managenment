using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class ReconciliationController(FeatureHubService hub, RoleGuard roleGuard) : Controller
{
    [HttpGet("/Reconciliation")]
    public async Task<IActionResult> Index()
    {
        if (!PortalCapabilities.CanReconcile(GetPortal()))
        {
            return Forbid();
        }

        ViewData["Title"] = "Reconciliation";
        return View(await hub.GetReconciliationSummaryAsync());
    }

    private string GetPortal()
        => PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
}
