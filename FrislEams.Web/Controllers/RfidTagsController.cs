using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class RfidTagsController(AppDbContext db, RfidTagService rfidTagService, RoleGuard roleGuard) : Controller
{
    private bool CanManage() =>
        roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.Backoffice);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!CanManage()) return Forbid();
        var tags = await rfidTagService.GetAllAsync();
        var assets = await db.Assets.AsQueryable().ToDictionaryAsync(a => a.Id, a => a);
        ViewBag.Assets = assets;
        return View(tags);
    }

    [HttpGet]
    public async Task<IActionResult> Assign(int assetId)
    {
        if (!CanManage()) return Forbid();
        var asset = await db.Assets.FindAsync(assetId);
        if (asset is null) return NotFound();
        ViewBag.Asset = asset;
        ViewBag.ExistingTag = await rfidTagService.FindByAssetIdAsync(assetId);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int assetId, string rfidCode, string action = "assign")
    {
        if (!CanManage()) return Forbid();
        var username = HttpContext.Session.GetString("LoginUsername") ?? "system";
        var (success, message, _) = action == "write"
            ? await rfidTagService.WriteTagAsync(assetId, username)
            : await rfidTagService.AssignToAssetAsync(assetId, rfidCode.Trim(), username);

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Detail", "Assets", new { id = assetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(int assetId, string newRfidCode)
    {
        if (!CanManage()) return Forbid();
        var username = HttpContext.Session.GetString("LoginUsername") ?? "system";
        var (success, message) = await rfidTagService.ReplaceTagAsync(assetId, newRfidCode.Trim(), username);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Detail", "Assets", new { id = assetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int assetId)
    {
        if (!CanManage()) return Forbid();
        var username = HttpContext.Session.GetString("LoginUsername") ?? "system";
        var (success, message) = await rfidTagService.RemoveTagAsync(assetId, username);
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction("Detail", "Assets", new { id = assetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string tagStatus)
    {
        if (!CanManage()) return Forbid();
        if (!RfidTagStatus.All.Contains(tagStatus)) return BadRequest();
        var tag = await db.RfidTags.FindAsync(id);
        if (tag is null) return NotFound();
        tag.TagStatus = tagStatus;
        await db.SaveChangesAsync();
        TempData["Success"] = "Tag status updated.";
        return RedirectToAction(nameof(Index));
    }
}
