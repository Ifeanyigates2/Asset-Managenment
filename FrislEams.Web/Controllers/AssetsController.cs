using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class AssetsController(
    AppDbContext db,
    TagCodeGenerator tagCodeGenerator,
    AssetLifecycleService lifecycleService,
    RoleGuard roleGuard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int? category, int? department)
    {
        var query = db.Assets
            .Include(a => a.AssetCategory)
            .Include(a => a.CurrentLocation)
            .Include(a => a.CurrentDepartment)
            .Include(a => a.CurrentCustodian)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.TagCode.Contains(q) || a.AssetName.Contains(q) || (a.SerialNumber != null && a.SerialNumber.Contains(q)));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AssetStatus>(status, out var parsedStatus))
            query = query.Where(a => a.CurrentStatus == parsedStatus);
        if (category.HasValue)
            query = query.Where(a => a.AssetCategoryId == category.Value);
        if (department.HasValue)
            query = query.Where(a => a.CurrentDepartmentId == department.Value);

        ViewBag.Categories = await db.AssetCategories.ToListAsync();
        ViewBag.Departments = await db.Departments.ToListAsync();
        ViewBag.Filter = new { q, status, category, department };
        ViewBag.TotalCount = await query.CountAsync();
        var allCosts = await query.Where(a => a.PurchaseCost.HasValue).Select(a => a.PurchaseCost!.Value).ToListAsync();
        ViewBag.TotalValue = allCosts.Sum();

        var assets = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        return View(assets);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var asset = await db.Assets
            .Include(a => a.AssetCategory)
            .Include(a => a.CurrentLocation)
            .Include(a => a.CurrentDepartment)
            .Include(a => a.CurrentCustodian)
            .Include(a => a.Supplier)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (asset is null) return NotFound();
        MongoHydrator.HydrateAssets([asset], db);

        ViewBag.History = await db.AssetStatusHistories
            .Where(h => h.AssetId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

        var assignments = await db.AssetAssignments
            .Include(a => a.AssignedToStaff)
            .Include(a => a.AssignedLocation)
            .Where(a => a.AssetId == id)
            .OrderByDescending(a => a.AssignedDate)
            .ToListAsync();
        MongoHydrator.HydrateAssignments(assignments, db);
        ViewBag.Assignments = assignments;

        ViewBag.RfidTag = await db.RfidTags.AsQueryable().FirstOrDefaultAsync(r => r.AssetId == id && r.IsActive);
        var repairRequests = await db.RepairRequests
            .Include(r => r.AssignedContractor)
            .Where(r => r.AssetId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        MongoHydrator.HydrateRepairRequests(repairRequests, db);
        ViewBag.RepairRequests = repairRequests;

        return View(asset);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice))
            return Forbid();

        ViewBag.Categories = await db.AssetCategories.ToListAsync();
        ViewBag.Locations = await db.Locations.ToListAsync();
        ViewBag.Departments = await db.Departments.ToListAsync();
        ViewBag.Suppliers = await db.Suppliers.ToListAsync();
        return View(new AssetRegistrationVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AssetRegistrationVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice))
            return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await db.AssetCategories.ToListAsync();
            ViewBag.Locations = await db.Locations.ToListAsync();
            ViewBag.Departments = await db.Departments.ToListAsync();
            ViewBag.Suppliers = await db.Suppliers.ToListAsync();
            return View(vm);
        }

        var rfidCode = vm.RfidCode?.Trim();
        if (!string.IsNullOrWhiteSpace(rfidCode))
        {
            var existingRfid = await db.RfidTags.AsQueryable().AnyAsync(r => r.RfidCode == rfidCode);
            if (existingRfid)
            {
                ModelState.AddModelError(nameof(vm.RfidCode), "This RFID code is already registered.");
                ViewBag.Categories = await db.AssetCategories.ToListAsync();
                ViewBag.Locations = await db.Locations.ToListAsync();
                ViewBag.Departments = await db.Departments.ToListAsync();
                ViewBag.Suppliers = await db.Suppliers.ToListAsync();
                return View(vm);
            }
        }

        var asset = new Asset
        {
            TagCode = tagCodeGenerator.Generate(vm.AssetCategoryId, vm.InitialDepartmentId),
            AssetName = vm.AssetName,
            Description = vm.Description,
            AssetCategoryId = vm.AssetCategoryId,
            PurchaseDate = vm.PurchaseDate,
            PurchaseCost = vm.PurchaseCost,
            GlCode = vm.GlCode,
            StateOfPurchase = vm.StateOfPurchase,
            SupplierId = vm.SupplierId,
            SerialNumber = vm.SerialNumber,
            ModelNumber = vm.ModelNumber,
            Brand = vm.Brand,
            TagNumber = vm.TagNumber,
            WarrantyExpiryDate = vm.WarrantyExpiryDate,
            ExpectedServiceYears = vm.ExpectedServiceYears,
            CurrentCondition = vm.CurrentCondition,
            CurrentLocationId = vm.InitialLocationId,
            CurrentDepartmentId = vm.InitialDepartmentId,
            Notes = vm.Notes,
            CurrentStatus = AssetStatus.UnregisteredUnassigned
        };

        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(rfidCode))
        {
            db.RfidTags.Add(new RfidTag { AssetId = asset.Id, RfidCode = rfidCode, TagStatus = RfidTagStatus.Active, IsActive = true });
        }
        lifecycleService.ChangeStatus(asset, AssetStatus.RegisteredUnassigned, "Asset registered into EAMS", "Admin");

        var missingPurchaseCost = !asset.PurchaseCost.HasValue;
        var missingGlCode = string.IsNullOrWhiteSpace(asset.GlCode);
        if (missingPurchaseCost || missingGlCode)
        {
            var missingParts = new List<string>();
            if (missingPurchaseCost) missingParts.Add("purchase cost");
            if (missingGlCode) missingParts.Add("GL code");

            db.Notifications.Add(new Notification
            {
                TargetRole = RoleName.Backoffice,
                Title = "Asset Financial Details Need Update",
                Message = $"Asset {asset.TagCode} ({asset.AssetName}) was registered without {string.Join(" and ", missingParts)}. Please update it as soon as possible.",
                Type = "Warning",
                LinkUrl = $"/Assets/Detail/{asset.Id}",
                IsRead = false
            });
        }

        await db.SaveChangesAsync();

        if (missingPurchaseCost || missingGlCode)
        {
            var reminderItems = new List<string>();
            if (missingPurchaseCost) reminderItems.Add("purchase cost");
            if (missingGlCode) reminderItems.Add("GL code");
            TempData["Success"] = $"Asset registered with tag code {asset.TagCode}. It now appears in the asset register. Please update the missing {string.Join(" and ", reminderItems)} as soon as possible.";
        }
        else
        {
            TempData["Success"] = $"Asset registered with tag code {asset.TagCode}. It now appears in the asset register.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(StatusChangeVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice)) return Forbid();

        var asset = await db.Assets.FindAsync(vm.AssetId);
        if (asset is null) return NotFound();

        var ok = lifecycleService.ChangeStatus(asset, vm.NextStatus, vm.Reason, vm.ChangedBy);
        if (!ok)
        {
            TempData["Error"] = "Invalid status transition.";
            return RedirectToAction(nameof(Index));
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Asset status changed to {vm.NextStatus}.";
        return RedirectToAction(nameof(Detail), new { id = vm.AssetId });
    }

    [HttpGet]
    public async Task<IActionResult> History(int id)
    {
        var history = await db.AssetStatusHistories
            .Where(h => h.AssetId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
        var asset = await db.Assets
            .Include(a => a.AssetCategory)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (asset is not null)
        {
            MongoHydrator.HydrateAssets([asset], db);
        }

        ViewBag.Asset = asset;
        return View(history);
    }
}
