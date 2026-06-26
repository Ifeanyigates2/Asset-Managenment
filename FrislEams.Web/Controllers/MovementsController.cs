using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class MovementsController(FeatureHubService hub, RoleGuard roleGuard) : Controller
{
    [HttpGet("/Movements")]
    public async Task<IActionResult> Index()
    {
        if (!PortalCapabilities.CanManageOperations(GetPortal()))
        {
            return Forbid();
        }

        ViewData["Title"] = "Movements";
        return View(await hub.GetRecentMovementsAsync());
    }

    private string GetPortal()
        => PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
}
