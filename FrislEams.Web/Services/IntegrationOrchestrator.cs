using System.Text.Json;
using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class IntegrationOrchestrator(AppDbContext db, RfidMonitoringService rfidMonitoringService)
{
    public async Task ProcessAsync(IntegrationJob job, CancellationToken cancellationToken = default)
    {
        var log = new IntegrationEventLog
        {
            EventType = job.EventType,
            SourceSystem = job.SourceSystem,
            Payload = job.Payload,
            ProcessingStatus = "Processing"
        };

        db.IntegrationEventLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            if (job.EventType == "ProcurementSync")
            {
                await ProcessProcurementSyncAsync(job.Payload, cancellationToken);
            }
            else if (job.EventType == "RfidBatchScan")
            {
                await ProcessRfidBatchAsync(job.Payload);
            }
            else
            {
                throw new InvalidOperationException($"Unknown integration event type: {job.EventType}");
            }

            log.ProcessingStatus = "Processed";
            log.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            log.ProcessingStatus = "Failed";
            log.ProcessingNote = ex.Message;
            log.ProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessProcurementSyncAsync(string payload, CancellationToken cancellationToken)
    {
        var vm = JsonSerializer.Deserialize<ProcurementSyncVm>(payload)
            ?? throw new InvalidOperationException("Invalid procurement payload.");

        foreach (var item in vm.Items)
        {
            db.ProcurementRecords.Add(new ProcurementRecord
            {
                ExternalReference = item.ExternalReference,
                VendorName = item.VendorName,
                VendorCode = item.VendorCode,
                AssetName = item.AssetName,
                CategoryId = item.CategoryId,
                UnitCost = item.UnitCost,
                Quantity = item.Quantity,
                GlCode = item.GlCode,
                PurchaseDate = item.PurchaseDate,
                Status = "Synced"
            });

            var storageId = db.Locations.AsQueryable().Where(l => l.Code == "STORAGE").Select(l => l.Id).FirstOrDefault();

            for (var i = 0; i < item.Quantity; i++)
            {
                db.Assets.Add(new Asset
                {
                    TagCode = $"ERP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                    AssetName = item.AssetName,
                    AssetCategoryId = item.CategoryId,
                    PurchaseDate = item.PurchaseDate,
                    PurchaseCost = item.UnitCost,
                    GlCode = item.GlCode,
                    CurrentStatus = AssetStatus.UnderProcurement,
                    CurrentLocationId = storageId,
                    CurrentCondition = "New",
                    Description = $"Synced from {vm.SourceSystem}"
                });
            }

            db.Notifications.Add(new Notification
            {
                TargetRole = RoleName.Admin,
                Message = $"Procurement sync created {item.Quantity} asset placeholder(s) for {item.AssetName}"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessRfidBatchAsync(string payload)
    {
        var vm = JsonSerializer.Deserialize<RfidBatchScanVm>(payload)
            ?? throw new InvalidOperationException("Invalid RFID batch payload.");

        foreach (var scan in vm.Scans)
        {
            await rfidMonitoringService.ProcessDoorScanAsync(scan.RfidCode, scan.DoorLocation);
        }
    }
}
