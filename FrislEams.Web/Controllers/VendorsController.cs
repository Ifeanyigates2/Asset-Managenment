using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class VendorsController(MongoVendorService vendorService, RoleGuard roleGuard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var vendors = await vendorService.GetAllAsync();
        return View(vendors);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        return View(new VendorVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        if (!ModelState.IsValid)
            return View(vm);

        if (await vendorService.CodeExistsAsync(vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "A vendor with this code already exists.");
            return View(vm);
        }

        await vendorService.CreateAsync(vm);
        TempData["Success"] = $"Vendor '{vm.Name}' registered in MongoDB.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var vendor = await vendorService.GetByIdAsync(id);
        return vendor is null ? NotFound() : View(vendor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, VendorVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        vm.Id = id;
        if (!ModelState.IsValid)
            return View(vm);

        if (await vendorService.CodeExistsAsync(vm.Code, id))
        {
            ModelState.AddModelError(nameof(vm.Code), "A vendor with this code already exists.");
            return View(vm);
        }

        await vendorService.UpdateAsync(vm);
        TempData["Success"] = "Vendor updated in MongoDB.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var vendor = await vendorService.ToggleActiveAsync(id);
        if (vendor is null) return NotFound();

        TempData["Success"] = $"Vendor '{vendor.Name}' {(vendor.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }
}
