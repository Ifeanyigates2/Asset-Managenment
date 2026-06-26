using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class UserRolesController(AppDbContext db, RoleGuard roleGuard) : Controller
{
    private static readonly string[] RoleOptions =
    [
        RoleName.Admin,
        RoleName.Backoffice,
        RoleName.Auditor,
        RoleName.Staff,
        RoleName.DepartmentHead,
        RoleName.Viewer,
        RoleName.Supplier,
        RoleName.RepairContractor
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var users = await db.UserAccounts
            .OrderBy(u => u.Username)
            .ToListAsync();

        ViewBag.RoleOptions = RoleOptions;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int id, string role)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        if (!RoleOptions.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Please choose a valid role.";
            return RedirectToAction(nameof(Index));
        }

        var user = await db.UserAccounts.FindAsync(id);
        if (user is null)
            return NotFound();

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Role for '{user.DisplayName}' updated to {role}.";
        return RedirectToAction(nameof(Index));
    }
}
