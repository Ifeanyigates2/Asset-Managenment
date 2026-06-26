using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class StaffController(AppDbContext db, RoleGuard roleGuard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var staff = await db.Staff
            .Include(s => s.Department)
            .OrderBy(s => s.FullName)
            .ToListAsync();
        ViewBag.Departments = await db.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(staff);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        ViewBag.Departments = await db.Departments.ToListAsync();
        return View(new Staff());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Staff vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Departments = await db.Departments.ToListAsync();
            return View(vm);
        }

        vm.JoinedAt = DateTime.UtcNow;
        db.Staff.Add(vm);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Staff member '{vm.FullName}' added.";
        return RedirectToAction(nameof(Index));
    }
}
