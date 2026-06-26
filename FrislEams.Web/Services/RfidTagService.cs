using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class RfidTagService(AppDbContext db, SystemAuditService audit)
{
    public async Task<List<RfidTag>> GetAllAsync()
        => await db.RfidTags.AsQueryable().OrderByDescending(t => t.RegisteredAt).ToListAsync();

    public async Task<RfidTag?> FindByCodeAsync(string rfidCode)
        => await db.RfidTags.AsQueryable().FirstOrDefaultAsync(t => t.RfidCode == rfidCode);

    public async Task<RfidTag?> FindByAssetIdAsync(int assetId)
        => await db.RfidTags.AsQueryable().FirstOrDefaultAsync(t => t.AssetId == assetId && t.IsActive);

    public async Task<(bool Success, string Message, RfidTag? Tag)> AssignToAssetAsync(int assetId, string rfidCode, string username)
    {
        var asset = await db.Assets.FindAsync(assetId);
        if (asset is null) return (false, "Asset not found.", null);

        var existingCode = await FindByCodeAsync(rfidCode);
        if (existingCode is not null && existingCode.AssetId != assetId)
            return (false, "This RFID tag is already linked to another asset.", null);

        var existingAssetTag = await FindByAssetIdAsync(assetId);
        if (existingAssetTag is not null && existingAssetTag.RfidCode != rfidCode)
            return (false, "Asset already has an active RFID tag. Use Replace instead.", null);

        var tag = existingCode ?? new RfidTag { RfidCode = rfidCode };
        tag.AssetId = assetId;
        tag.TagStatus = RfidTagStatus.Active;
        tag.IsActive = true;
        tag.EncodedBy = username;
        tag.EncodedAt = DateTime.UtcNow;

        if (existingCode is null)
            db.RfidTags.Add(tag);

        await db.SaveChangesAsync();
        await audit.LogAsync(username, "Assign RFID Tag", "Asset", assetId.ToString(), null, rfidCode);
        return (true, "RFID tag assigned and activated.", tag);
    }

    public async Task<(bool Success, string Message)> ReplaceTagAsync(int assetId, string newRfidCode, string username)
    {
        var oldTag = await FindByAssetIdAsync(assetId);
        if (oldTag is not null)
        {
            oldTag.IsActive = false;
            oldTag.TagStatus = RfidTagStatus.Retired;
        }

        var result = await AssignToAssetAsync(assetId, newRfidCode, username);
        return (result.Success, result.Message);
    }

    public async Task<(bool Success, string Message)> RemoveTagAsync(int assetId, string username)
    {
        var tag = await FindByAssetIdAsync(assetId);
        if (tag is null) return (false, "No active RFID tag on this asset.");

        var code = tag.RfidCode;
        tag.IsActive = false;
        tag.TagStatus = RfidTagStatus.Retired;
        tag.AssetId = null;
        await db.SaveChangesAsync();
        await audit.LogAsync(username, "Remove RFID Tag", "Asset", assetId.ToString(), code, null);
        return (true, "RFID tag removed from asset.");
    }

    public async Task<(bool Success, string Message, Asset? Asset)> VerifyTagAsync(string rfidCode)
    {
        var tag = await FindByCodeAsync(rfidCode);
        if (tag?.AssetId is null || !tag.IsActive)
            return (false, "Unknown or inactive RFID tag.", null);

        var asset = await db.Assets.FindAsync(tag.AssetId.Value);
        if (asset is null) return (false, "Linked asset record not found.", null);

        MongoHydrator.HydrateAssets([asset], db);
        return (true, "Tag verified successfully.", asset);
    }

    public async Task<string> GenerateEpcAsync()
    {
        var seq = await db.RfidTags.AsQueryable().CountAsync() + 1;
        return $"EPC-{DateTime.UtcNow:yyyyMMdd}-{seq:D6}";
    }

    public async Task<(bool Success, string Message, RfidTag? Tag)> WriteTagAsync(int assetId, string username)
    {
        var epc = await GenerateEpcAsync();
        return await AssignToAssetAsync(assetId, epc, username);
    }
}
