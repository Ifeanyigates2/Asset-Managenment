using FrislEams.Web.Data;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class NotificationsController(AppDbContext db, FeatureHubService hub, RoleGuard roleGuard) : Controller
{
    [HttpGet("/Notifications")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Notifications";
        var role = roleGuard.GetCurrentRole(HttpContext);
        return View(await hub.GetNotificationsForRoleAsync(role));
    }

    [HttpPost("/Notifications/MarkRead/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var notification = await db.Notifications.FindAsync(id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        db.Notifications.Update(notification);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
