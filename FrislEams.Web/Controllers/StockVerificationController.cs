using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class StockVerificationController(AppDbContext db, StockVerificationService stockService, RoleGuard roleGuard) : Controller
{
    private bool CanRun() =>
        roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice, RoleName.Auditor);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!CanRun()) return Forbid();
        var sessions = await db.StockVerificationSessions.AsQueryable().OrderByDescending(s => s.StartedAt).Take(20).ToListAsync();
        ViewBag.Departments = await db.Departments.AsQueryable().OrderBy(d => d.Name).ToListAsync();
        ViewBag.Locations = await db.Locations.AsQueryable().OrderBy(l => l.Name).ToListAsync();
        return View(sessions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int? departmentId, int? locationId)
    {
        if (!CanRun()) return Forbid();
        var username = HttpContext.Session.GetString("UserName") ?? "User";
        var session = await stockService.StartSessionAsync(departmentId, locationId, username);
        return RedirectToAction(nameof(Session), new { id = session.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Session(int id)
    {
        if (!CanRun()) return Forbid();
        var session = await db.StockVerificationSessions.FindAsync(id);
        if (session is null) return NotFound();
        ViewBag.Scans = await stockService.GetScansAsync(id);
        return View(session);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan(int sessionId, string rfidCode)
    {
        if (!CanRun()) return Forbid();
        var username = HttpContext.Session.GetString("UserName") ?? "User";
        var (success, message, _) = await stockService.ProcessScanAsync(sessionId, rfidCode.Trim(), username);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Session), new { id = sessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int sessionId)
    {
        if (!CanRun()) return Forbid();
        var username = HttpContext.Session.GetString("UserName") ?? "User";
        await stockService.CompleteSessionAsync(sessionId, username);
        TempData["Success"] = "Stock verification completed. Report generated.";
        return RedirectToAction(nameof(Session), new { id = sessionId });
    }
}
