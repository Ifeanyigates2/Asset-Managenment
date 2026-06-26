using FrislEams.Web.Data;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class RfidReaderController(AppDbContext db, RfidTagService rfidTagService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string mode = "read")
    {
        ViewBag.Mode = mode.ToLowerInvariant();
        if (mode.Equals("write", StringComparison.OrdinalIgnoreCase))
            ViewBag.Assets = await db.Assets.AsQueryable().OrderBy(a => a.AssetName).Take(100).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan(string mode, string rfidCode, int? assetId)
    {
        mode = mode.ToLowerInvariant();
        var username = HttpContext.Session.GetString("LoginUsername") ?? "system";

        switch (mode)
        {
            case "read":
            case "verify":
            {
                var (success, message, asset) = await rfidTagService.VerifyTagAsync(rfidCode.Trim());
                if (!success)
                {
                    ViewBag.Mode = mode;
                    ViewBag.Error = message;
                    ViewBag.UnknownTag = rfidCode.Trim();
                    ViewBag.LinkAssets = await db.Assets.AsQueryable().OrderBy(a => a.AssetName).Take(50).ToListAsync();
                    return View("Index");
                }

                MongoHydrator.HydrateAssets([asset!], db);
                ViewBag.Mode = mode;
                ViewBag.Asset = asset;
                ViewBag.Message = message;
                return View("Index");
            }
            case "write":
            {
                if (!assetId.HasValue)
                {
                    ViewBag.Mode = mode;
                    ViewBag.Error = "Select an asset before writing a tag.";
                    ViewBag.Assets = await db.Assets.AsQueryable().OrderBy(a => a.AssetName).Take(100).ToListAsync();
                    return View("Index");
                }

                var (success, message, tag) = await rfidTagService.WriteTagAsync(assetId.Value, username);
                ViewBag.Mode = mode;
                ViewBag.Message = message;
                ViewBag.WrittenTag = tag;
                if (!success) ViewBag.Error = message;
                return View("Index");
            }
            default:
                return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleUnknown(string rfidCode, string action, int? assetId)
    {
        if (action == "link" && assetId.HasValue)
        {
            var username = HttpContext.Session.GetString("LoginUsername") ?? "system";
            var (success, message, _) = await rfidTagService.AssignToAssetAsync(assetId.Value, rfidCode.Trim(), username);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Detail", "Assets", new { id = assetId });
        }

        if (action == "register")
        {
            TempData["Success"] = "Navigate to Assets → New Asset to register this as a new asset, then assign the RFID tag.";
            return RedirectToAction("Register", "Assets");
        }

        TempData["Success"] = "Unknown tag ignored.";
        return RedirectToAction(nameof(Index));
    }
}
