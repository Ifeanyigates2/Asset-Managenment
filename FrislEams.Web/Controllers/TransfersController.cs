using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class TransfersController(AppDbContext db, RoleGuard roleGuard, SystemAuditService audit) : Controller
{
    private bool CanManage() =>
        roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice, RoleName.Staff);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!CanManage()) return Forbid();
        var transfers = await db.AssetTransfers.AsQueryable().OrderByDescending(t => t.CreatedAt).ToListAsync();
        return View(transfers);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assetId)
    {
        if (!CanManage()) return Forbid();
        ViewBag.Assets = await db.Assets.AsQueryable().OrderBy(a => a.AssetName).ToListAsync();
        ViewBag.Departments = await db.Departments.AsQueryable().OrderBy(d => d.Name).ToListAsync();
        ViewBag.Locations = await db.Locations.AsQueryable().OrderBy(l => l.Name).ToListAsync();
        return View(new TransferCreateVm { AssetId = assetId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferCreateVm vm)
    {
        if (!CanManage()) return Forbid();
        if (!ModelState.IsValid) return await Create(vm.AssetId);

        var asset = await db.Assets.FindAsync(vm.AssetId);
        if (asset is null) return NotFound();

        var transfer = new AssetTransfer
        {
            AssetId = vm.AssetId,
            FromDepartmentId = asset.CurrentDepartmentId ?? 0,
            ToDepartmentId = vm.ToDepartmentId,
            FromLocationId = asset.CurrentLocationId,
            ToLocationId = vm.ToLocationId,
            Status = "Pending",
            RequestedBy = HttpContext.Session.GetString("UserName") ?? "User"
        };
        db.AssetTransfers.Add(transfer);
        await db.SaveChangesAsync();
        await audit.LogAsync(HttpContext.Session.GetString("LoginUsername") ?? "system", "Request Transfer", "AssetTransfer", transfer.Id.ToString());
        TempData["Success"] = "Transfer request submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice)) return Forbid();
        var transfer = await db.AssetTransfers.FindAsync(id);
        if (transfer is null) return NotFound();
        transfer.Status = "Approved";
        transfer.ApprovedBy = HttpContext.Session.GetString("UserName");
        transfer.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Transfer approved. Scan asset at departure and arrival.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanDeparture(int id, string rfidCode)
    {
        var transfer = await db.AssetTransfers.FindAsync(id);
        if (transfer is null) return NotFound();
        var tag = await db.RfidTags.AsQueryable().FirstOrDefaultAsync(t => t.AssetId == transfer.AssetId && t.IsActive);
        if (tag is null || !string.Equals(tag.RfidCode, rfidCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "RFID scan does not match the asset for this transfer.";
            return RedirectToAction(nameof(Index));
        }

        transfer.DepartureRfidScan = rfidCode.Trim();
        transfer.DepartureScannedAt = DateTime.UtcNow;
        transfer.Status = "Departed";
        await db.SaveChangesAsync();
        TempData["Success"] = "Departure scan recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanArrival(int id, string rfidCode)
    {
        var transfer = await db.AssetTransfers.FindAsync(id);
        if (transfer is null) return NotFound();
        var tag = await db.RfidTags.AsQueryable().FirstOrDefaultAsync(t => t.AssetId == transfer.AssetId && t.IsActive);
        if (tag is null || !string.Equals(tag.RfidCode, rfidCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "RFID scan does not match the asset for this transfer.";
            return RedirectToAction(nameof(Index));
        }

        var asset = await db.Assets.FindAsync(transfer.AssetId);
        if (asset is not null)
        {
            asset.CurrentDepartmentId = transfer.ToDepartmentId;
            if (transfer.ToLocationId.HasValue)
                asset.CurrentLocationId = transfer.ToLocationId;
        }

        transfer.ArrivalRfidScan = rfidCode.Trim();
        transfer.ArrivalScannedAt = DateTime.UtcNow;
        transfer.Status = "Completed";
        await db.SaveChangesAsync();
        await audit.LogAsync(HttpContext.Session.GetString("LoginUsername") ?? "system", "Complete Transfer", "AssetTransfer", id.ToString());
        TempData["Success"] = "Arrival scan recorded. Transfer completed.";
        return RedirectToAction(nameof(Index));
    }
}
