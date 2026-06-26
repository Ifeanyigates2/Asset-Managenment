using System.Text;
using FrislEams.Web.Data;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class ReportsController(AppDbContext db, ReportingService reportingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ReportFilterVm filter)
    {
        ViewBag.Departments = await db.Departments.ToListAsync();
        ViewBag.Categories = await db.AssetCategories.ToListAsync();
        ViewBag.Filter = filter;
        var report = await reportingService.GetAssetsReportAsync(filter);
        ViewBag.TotalValue = report.Where(a => a.PurchaseCost.HasValue).Sum(a => a.PurchaseCost!.Value);
        ViewBag.TotalCount = report.Count;
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(ReportFilterVm filter)
    {
        var report = await reportingService.GetAssetsReportAsync(filter);
        var csv = reportingService.BuildAssetsCsv(report);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"assets-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Depreciation()
    {
        var report = await reportingService.GetDepreciationReportAsync();
        ViewBag.TotalCost = report.Sum(r => r.PurchaseCost);
        ViewBag.TotalNbv = report.Sum(r => r.NetBookValue);
        ViewBag.TotalAccumulated = report.Sum(r => r.AccumulatedDepreciation);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportDepreciationCsv()
    {
        var report = await reportingService.GetDepreciationReportAsync();
        var csv = reportingService.BuildDepreciationCsv(report);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"depreciation-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Aging()
    {
        var report = await reportingService.GetAgingReportAsync();
        ViewBag.BandSummary = report.GroupBy(r => r.AgeBand).ToDictionary(g => g.Key, g => g.Count());
        ViewBag.WarrantyExpired = report.Count(r => r.WarrantyExpired);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAgingCsv()
    {
        var report = await reportingService.GetAgingReportAsync();
        var csv = reportingService.BuildAgingCsv(report);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"asset-aging-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
