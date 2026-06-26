using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class RfidMonitoringService(AppDbContext db)
{
    public async Task<(string Outcome, bool Alert, string Message)> ProcessDoorScanAsync(string rfidCode, string doorLocation)
    {
        var tag = await db.RfidTags.AsQueryable()
            .Where(r => r.RfidCode == rfidCode && r.IsActive)
            .FirstOrDefaultAsync();
        if (tag is not null)
        {
            MongoHydrator.HydrateRfidTags([tag], db);
        }

        if (tag is null)
        {
            var alertMsg = $"Unregistered RFID tag '{rfidCode}' detected at {doorLocation}. Possible unauthorized equipment.";
            db.RfidEvents.Add(new RfidEvent
            {
                RfidCode = rfidCode,
                EventType = "ExternalAsset",
                DoorLocation = doorLocation,
                ProcessedStatus = "Unknown RFID – logged as external",
                AlertTriggered = true,
                AlertMessage = alertMsg,
                EventTime = DateTime.UtcNow
            });
            db.Notifications.Add(new Notification
            {
                TargetRole = RoleName.Admin,
                Title = "Unknown RFID Exit Detected",
                Message = alertMsg,
                Type = "Alert",
                IsRead = false
            });
            await db.SaveChangesAsync();
            return ("Unknown RFID – alert triggered", true, alertMsg);
        }

        var asset = tag.Asset!;
        var activeGrant = await db.ExitGrants.AsQueryable().AnyAsync(g => g.AssetId == asset.Id && g.IsActive && g.GrantEndDate >= DateTime.UtcNow);
        var isLoanedOut = asset.CurrentStatus is AssetStatus.LoanedOutExternal or AssetStatus.LoanedOutInternal;
        var allowed = activeGrant || isLoanedOut;

        if (allowed)
        {
            db.RfidEvents.Add(new RfidEvent
            {
                RfidCode = rfidCode,
                AssetId = asset.Id,
                EventType = "AuthorizedExitOrReturn",
                DoorLocation = doorLocation,
                ProcessedStatus = "Authorized",
                AlertTriggered = false,
                EventTime = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return ("Authorized movement logged", false, $"Asset {asset.TagCode} authorized at {doorLocation}.");
        }

        var unauthorizedMsg = $"UNAUTHORIZED exit attempt: Asset {asset.TagCode} ({asset.AssetName}) detected at {doorLocation} with no active exit grant.";
        db.RfidEvents.Add(new RfidEvent
        {
            RfidCode = rfidCode,
            AssetId = asset.Id,
            EventType = "UnauthorizedAttempt",
            DoorLocation = doorLocation,
            ProcessedStatus = "Blocked – no active exit grant",
            AlertTriggered = true,
            AlertMessage = unauthorizedMsg,
            EventTime = DateTime.UtcNow
        });
        db.Notifications.Add(new Notification
        {
            TargetRole = RoleName.Admin,
            Title = "Unauthorized Asset Exit Attempt",
            Message = unauthorizedMsg,
            Type = "Alert",
            IsRead = false
        });
        await db.SaveChangesAsync();
        return ("Unauthorized attempt – alert triggered", true, unauthorizedMsg);
    }
}
