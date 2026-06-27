using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class AuditScanController(AuditScanService auditScanService, RoleGuard roleGuard) : Controller
{
    private bool CanRun() =>
        roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice, RoleName.Auditor);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!CanRun()) return Forbid();
        var periods = await auditScanService.GetPeriodsAsync();
        return View(periods);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuditScanPeriodCreateVm vm)
    {
        if (!CanRun()) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please provide a name and valid date range.";
            return RedirectToAction(nameof(Index));
        }

        var username = HttpContext.Session.GetString("UserName") ?? "Auditor";
        try
        {
            var period = await auditScanService.CreatePeriodAsync(vm.Name, vm.StartDate, vm.EndDate, username);
            TempData["Success"] = $"Audit period \"{period.Name}\" created.";
            return RedirectToAction(nameof(Period), new { id = period.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Period(int id)
    {
        if (!CanRun()) return Forbid();
        var period = await auditScanService.GetPeriodAsync(id);
        if (period is null) return NotFound();
        ViewBag.Scans = await auditScanService.GetTemporaryScanListAsync(id);
        return View(period);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadScan(int periodId, int scanNumber, string rfidCodes)
    {
        if (!CanRun()) return Forbid();
        var username = HttpContext.Session.GetString("UserName") ?? "Auditor";
        var codes = rfidCodes
            .Split(new[] { '\n', '\r', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        var (success, message, _) = await auditScanService.AddScanBatchAsync(
            periodId, scanNumber, codes, "manual_upload", username);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Period), new { id = periodId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckDiscrepancies(int periodId)
    {
        if (!CanRun()) return Forbid();
        var username = HttpContext.Session.GetString("UserName") ?? "Auditor";
        try
        {
            var result = await auditScanService.CheckDiscrepanciesAsync(periodId, username);
            TempData["Success"] = $"Discrepancy check complete: {result.Concerns.Count} concern(s) from {result.DistinctScannedCount} distinct tag(s).";
            return RedirectToAction(nameof(Discrepancies), new { id = periodId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Period), new { id = periodId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Discrepancies(int id)
    {
        if (!CanRun()) return Forbid();
        var period = await auditScanService.GetPeriodAsync(id);
        if (period is null) return NotFound();
        ViewBag.Scans = await auditScanService.GetTemporaryScanListAsync(id);
        var concerns = await auditScanService.GetDiscrepanciesAsync(id);
        ViewBag.Period = period;
        return View(concerns);
    }
}
