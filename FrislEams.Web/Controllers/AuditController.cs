using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class AuditController(AppDbContext db, AuditService auditService, RoleGuard roleGuard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id)
    {
        ViewBag.Departments = await db.Departments.ToListAsync();
        ViewBag.Assets = await db.Assets.ToListAsync();
        ViewBag.ActiveSessionId = id;

        var sessions = await db.AuditSessions
            .Include(s => s.Department)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
        return View(sessions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartAuditVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Auditor, RoleName.DepartmentHead, RoleName.Admin))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var session = await auditService.StartSessionAsync(vm);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Audit session {session.Id} started.";
        return RedirectToAction(nameof(Index), new { id = session.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitResult(SubmitAuditResultVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Auditor, RoleName.DepartmentHead, RoleName.Admin))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index), new { id = vm.AuditSessionId });
        }

        await auditService.SubmitResultAsync(vm);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { id = vm.AuditSessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        await auditService.CloseSessionAsync(id);
        await db.SaveChangesAsync();
        TempData["Success"] = "Audit session closed.";
        return RedirectToAction(nameof(Variance), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Variance(int id)
    {
        var session = await db.AuditSessions.AsQueryable().Include(s => s.Department).FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return NotFound();
        }

        ViewBag.Session = session;
        ViewBag.Variance = await auditService.BuildVarianceAsync(id);

        var results = await db.AuditResults
            .Include(r => r.Asset)
            .Where(r => r.AuditSessionId == id)
            .OrderBy(r => r.AssetId)
            .ToListAsync();

        return View(results);
    }
}
