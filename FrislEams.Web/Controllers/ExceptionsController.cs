using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class ExceptionsController(FeatureHubService hub, RoleGuard roleGuard) : Controller
{
    [HttpGet("/Exceptions")]
    public async Task<IActionResult> Index()
    {
        if (!PortalCapabilities.CanReconcile(GetPortal()))
        {
            return Forbid();
        }

        ViewData["Title"] = "Exceptions";
        return View(await hub.GetExceptionsAsync());
    }

    private string GetPortal()
        => PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
}
