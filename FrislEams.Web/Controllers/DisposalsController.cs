using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class DisposalsController(FeatureHubService hub, RoleGuard roleGuard) : Controller
{
    [HttpGet("/Disposals")]
    public async Task<IActionResult> Index()
    {
        if (!PortalCapabilities.CanManageOperations(GetPortal()))
        {
            return Forbid();
        }

        ViewData["Title"] = "Disposals";
        return View(await hub.GetDisposedAssetsAsync());
    }

    private string GetPortal()
        => PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
}
