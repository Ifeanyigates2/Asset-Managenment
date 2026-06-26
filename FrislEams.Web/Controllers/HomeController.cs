using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class HomeController(RoleGuard roleGuard) : Controller
{
    public IActionResult Index()
    {
        var role = roleGuard.GetCurrentRole(HttpContext);
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToAction("Login", "Account");
        }

        return Redirect(PortalService.GetHomePath(role));
    }

    public IActionResult Error() => View();
}
