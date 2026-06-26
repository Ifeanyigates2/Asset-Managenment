using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class MasterDataController(AppDbContext db, RoleGuard roleGuard, SystemAuditService audit) : Controller
{
    private bool CanManage() =>
        roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice);

    [HttpGet]
    public async Task<IActionResult> Index(string section = "departments")
    {
        if (!CanManage()) return Forbid();
        ViewBag.Section = section;
        ViewBag.Departments = await db.Departments.AsQueryable().OrderBy(d => d.Name).ToListAsync();
        ViewBag.Categories = await db.AssetCategories.AsQueryable().OrderBy(c => c.Name).ToListAsync();
        ViewBag.AssetTypes = await db.AssetTypes.AsQueryable().OrderBy(t => t.Name).ToListAsync();
        ViewBag.Locations = await db.Locations.AsQueryable().OrderBy(l => l.Name).ToListAsync();
        ViewBag.Vendors = await db.Suppliers.AsQueryable().OrderBy(v => v.Name).ToListAsync();
        ViewBag.Manufacturers = await db.Manufacturers.AsQueryable().OrderBy(m => m.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string section, MasterDataItemVm vm)
    {
        if (!CanManage()) return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), new { section });

        var username = HttpContext.Session.GetString("LoginUsername") ?? "system";
        switch (section)
        {
            case "departments":
                db.Departments.Add(new Department { Name = vm.Name.Trim(), Code = vm.Code.Trim().ToUpperInvariant() });
                break;
            case "categories":
                db.AssetCategories.Add(new AssetCategory { Name = vm.Name.Trim(), Code = vm.Code.Trim().ToUpperInvariant(), UsefulLifeYears = 5 });
                break;
            case "assettypes":
                if (!vm.AssetCategoryId.HasValue) { TempData["Error"] = "Select a category."; return RedirectToAction(nameof(Index), new { section }); }
                db.AssetTypes.Add(new AssetType { Name = vm.Name.Trim(), Code = vm.Code.Trim().ToUpperInvariant(), AssetCategoryId = vm.AssetCategoryId.Value });
                break;
            case "locations":
                db.Locations.Add(new Location
                {
                    Name = vm.Name.Trim(),
                    Code = vm.Code.Trim().ToUpperInvariant(),
                    LocationType = vm.LocationType ?? "Branch",
                    ParentLocationId = vm.ParentLocationId
                });
                break;
            case "vendors":
                db.Suppliers.Add(new Supplier { Name = vm.Name.Trim(), Code = vm.Code.Trim().ToUpperInvariant(), IsActive = true });
                break;
            case "manufacturers":
                db.Manufacturers.Add(new Manufacturer { Name = vm.Name.Trim(), Code = vm.Code.Trim().ToUpperInvariant() });
                break;
            default:
                return BadRequest();
        }

        await db.SaveChangesAsync();
        await audit.LogAsync(username, $"Create {section}", section, vm.Code);
        TempData["Success"] = $"{vm.Name} added successfully.";
        return RedirectToAction(nameof(Index), new { section });
    }
}
