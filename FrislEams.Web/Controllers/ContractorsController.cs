using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class ContractorsController(AppDbContext db, RoleGuard roleGuard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var contractors = await db.RepairContractors
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(contractors);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        return View(new RepairContractor());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepairContractor vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        if (!ModelState.IsValid)
            return View(vm);

        vm.Code = vm.Code.Trim().ToUpper();
        var exists = await db.RepairContractors.AnyAsync(c => c.Code == vm.Code);
        if (exists)
        {
            ModelState.AddModelError(nameof(vm.Code), "A contractor with this code already exists.");
            return View(vm);
        }

        vm.RegisteredAt = DateTime.UtcNow;
        db.RepairContractors.Add(vm);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Contractor '{vm.Name}' registered successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var contractor = await db.RepairContractors.FindAsync(id);
        if (contractor is null) return NotFound();
        return View(contractor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RepairContractor vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var contractor = await db.RepairContractors.FindAsync(id);
        if (contractor is null) return NotFound();

        contractor.Name = vm.Name;
        contractor.ContactPerson = vm.ContactPerson;
        contractor.Phone = vm.Phone;
        contractor.Email = vm.Email;
        contractor.Specialisation = vm.Specialisation;
        contractor.SlaHours = vm.SlaHours;
        contractor.IsActive = vm.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = "Contractor updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin))
            return Forbid();

        var contractor = await db.RepairContractors.FindAsync(id);
        if (contractor is null) return NotFound();
        contractor.IsActive = !contractor.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Contractor '{contractor.Name}' {(contractor.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }
}
