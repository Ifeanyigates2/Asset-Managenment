using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class SettingsController(RoleGuard roleGuard) : Controller
{
    [HttpGet("/Settings")]
    public IActionResult Index()
    {
        var portal = PortalService.GetPortalForRole(roleGuard.GetCurrentRole(HttpContext));
        if (portal is not (PortalService.BackofficePortal or PortalService.AdminPortal))
        {
            return Forbid();
        }

        ViewData["Title"] = portal == PortalService.AdminPortal ? "System Settings" : "Settings";
        ViewBag.Portal = portal;
        return View();
    }
}
