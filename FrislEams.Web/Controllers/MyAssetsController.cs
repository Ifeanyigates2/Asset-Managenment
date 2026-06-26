using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class MyAssetsController(FeatureHubService hub) : Controller
{
    [HttpGet("/MyAssets")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "My Assets";
        var username = HttpContext.Session.GetString("LoginUsername");
        var displayName = HttpContext.Session.GetString("UserName");
        return View(await hub.GetStaffAssetsAsync(username, displayName));
    }
}
