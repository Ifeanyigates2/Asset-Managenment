using FrislEams.Web.Data;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class AuditScanService(AppDbContext db, SystemAuditService audit)
{
    public async Task<AuditScanPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate, string auditor)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("End date must be on or after start date.");
        }

        var period = new AuditScanPeriod
        {
            Name = name.Trim(),
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Auditor = auditor,
            Status = "Open"
        };
        db.AuditScanPeriods.Add(period);
        await db.SaveChangesAsync();
        await audit.LogAsync(auditor, "Create Audit Scan Period", "AuditScanPeriod", period.Id.ToString());
        return period;
    }

    public async Task<List<AuditScanPeriod>> GetPeriodsAsync(int take = 50)
        => await db.AuditScanPeriods.AsQueryable()
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<AuditScanPeriod?> GetPeriodAsync(int periodId)
        => await db.AuditScanPeriods.FindAsync(periodId);

    public async Task<List<AuditTemporaryScan>> GetTemporaryScanListAsync(int periodId)
        => await db.AuditTemporaryScans.AsQueryable()
            .Where(s => s.PeriodId == periodId)
            .OrderBy(s => s.ScanNumber)
            .ToListAsync();

    public async Task<(bool Success, string Message, AuditTemporaryScan? Scan)> AddScanBatchAsync(
        int periodId,
        int scanNumber,
        IEnumerable<string> rfidCodes,
        string source,
        string username)
    {
        if (scanNumber < 1)
        {
            return (false, "Scan number must be at least 1.", null);
        }

        var period = await db.AuditScanPeriods.FindAsync(periodId);
        if (period is null)
        {
            return (false, "Audit scan period not found.", null);
        }

        if (period.Status == "Closed")
        {
            return (false, "This audit period is closed.", null);
        }

        var normalized = rfidCodes
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (normalized.Count == 0)
        {
            return (false, "No RFID codes provided.", null);
        }

        var existingScan = await db.AuditTemporaryScans.AsQueryable()
            .FirstOrDefaultAsync(s => s.PeriodId == periodId && s.ScanNumber == scanNumber);

        if (existingScan is not null)
        {
            var oldItems = await db.AuditTemporaryScanItems.AsQueryable()
                .Where(i => i.ScanId == existingScan.Id)
                .ToListAsync();
            foreach (var item in oldItems)
            {
                db.AuditTemporaryScanItems.Remove(item);
            }
            db.AuditTemporaryScans.Remove(existingScan);
            await db.SaveChangesAsync();
        }

        var distinctInBatch = normalized
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        var duplicateInBatch = normalized.Count - distinctInBatch.Count;

        var tagLookup = await db.RfidTags.AsQueryable().ToListAsync();
        var tagByCode = tagLookup.ToDictionary(t => t.RfidCode, StringComparer.OrdinalIgnoreCase);

        var scan = new AuditTemporaryScan
        {
            PeriodId = periodId,
            ScanNumber = scanNumber,
            Source = source,
            ItemCount = normalized.Count,
            DuplicateInBatchCount = duplicateInBatch,
            ScannedAt = DateTime.UtcNow
        };
        db.AuditTemporaryScans.Add(scan);
        await db.SaveChangesAsync();

        foreach (var code in normalized)
        {
            tagByCode.TryGetValue(code, out var tag);
            db.AuditTemporaryScanItems.Add(new AuditTemporaryScanItem
            {
                ScanId = scan.Id,
                RfidCode = code,
                AssetId = tag?.AssetId,
                ScannedAt = DateTime.UtcNow
            });
        }

        if (period.Status == "DiscrepanciesChecked")
        {
            period.Status = "Open";
            period.DistinctScannedCount = null;
            period.DiscrepancyCount = null;
            var oldDiscrepancies = await db.AuditDiscrepancies.AsQueryable()
                .Where(d => d.PeriodId == periodId)
                .ToListAsync();
            foreach (var d in oldDiscrepancies)
            {
                db.AuditDiscrepancies.Remove(d);
            }
        }

        await db.SaveChangesAsync();
        await audit.LogAsync(username, $"Upload Audit Scan {scanNumber}", "AuditScanPeriod", periodId.ToString());
        return (true, $"Scan {scanNumber} saved with {normalized.Count} tag(s) ({distinctInBatch.Count} distinct).", scan);
    }

    public async Task<DiscrepancyCheckResult> CheckDiscrepanciesAsync(int periodId, string username)
    {
        var period = await db.AuditScanPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException("Audit scan period not found.");

        var scans = await db.AuditTemporaryScans.AsQueryable()
            .Where(s => s.PeriodId == periodId)
            .ToListAsync();
        if (scans.Count == 0)
        {
            throw new InvalidOperationException("No scans uploaded for this period. Upload at least Scan 1 before checking discrepancies.");
        }

        var scanIds = scans.Select(s => s.Id).ToHashSet();
        var allItems = await db.AuditTemporaryScanItems.AsQueryable()
            .Where(i => scanIds.Contains(i.ScanId))
            .ToListAsync();

        var existingDiscrepancies = await db.AuditDiscrepancies.AsQueryable()
            .Where(d => d.PeriodId == periodId)
            .ToListAsync();
        foreach (var d in existingDiscrepancies)
        {
            db.AuditDiscrepancies.Remove(d);
        }

        var concerns = new List<AuditDiscrepancy>();
        var distinctScanned = allItems
            .GroupBy(i => i.RfidCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var tags = await db.RfidTags.AsQueryable().ToListAsync();
        var tagByCode = tags.ToDictionary(t => t.RfidCode, StringComparer.OrdinalIgnoreCase);
        var assets = await db.Assets.AsQueryable().ToListAsync();
        var assetById = assets.ToDictionary(a => a.Id);

        foreach (var scan in scans.Where(s => s.DuplicateInBatchCount > 0))
        {
            concerns.Add(new AuditDiscrepancy
            {
                PeriodId = periodId,
                Type = "duplicate_in_batch",
                RfidCode = "",
                Message = $"Scan {scan.ScanNumber} contains {scan.DuplicateInBatchCount} duplicate read(s) within the batch."
            });
        }

        foreach (var item in distinctScanned)
        {
            if (!tagByCode.TryGetValue(item.RfidCode, out var tag))
            {
                concerns.Add(new AuditDiscrepancy
                {
                    PeriodId = periodId,
                    Type = "unknown_tag",
                    RfidCode = item.RfidCode,
                    Message = "Unknown RFID — tag is not registered in the system."
                });
                continue;
            }

            if (!tag.IsActive)
            {
                concerns.Add(new AuditDiscrepancy
                {
                    PeriodId = periodId,
                    Type = "inactive_tag",
                    RfidCode = item.RfidCode,
                    AssetId = tag.AssetId,
                    AssetTag = tag.AssetId is int aid && assetById.TryGetValue(aid, out var a) ? a.TagCode : null,
                    Message = "Scanned RFID tag is marked inactive in the system."
                });
            }

            if (tag.AssetId is null)
            {
                concerns.Add(new AuditDiscrepancy
                {
                    PeriodId = periodId,
                    Type = "unassigned_tag",
                    RfidCode = item.RfidCode,
                    Message = "RFID tag is registered but not assigned to any asset."
                });
            }
        }

        var scannedAssetIds = distinctScanned
            .Select(i => i.AssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var expectedTags = tags.Where(t => t.IsActive && t.AssetId.HasValue).ToList();
        foreach (var tag in expectedTags)
        {
            if (!scannedAssetIds.Contains(tag.AssetId!.Value))
            {
                assetById.TryGetValue(tag.AssetId.Value, out var asset);
                concerns.Add(new AuditDiscrepancy
                {
                    PeriodId = periodId,
                    Type = "not_in_scan",
                    RfidCode = tag.RfidCode,
                    AssetId = tag.AssetId,
                    AssetTag = asset?.TagCode,
                    Message = $"Asset {(asset?.TagCode ?? tag.AssetId.ToString())} was not detected in any scan for this period."
                });
            }
        }

        foreach (var concern in concerns)
        {
            db.AuditDiscrepancies.Add(concern);
        }

        period.Status = "DiscrepanciesChecked";
        period.DistinctScannedCount = distinctScanned.Count;
        period.DiscrepancyCount = concerns.Count;
        await db.SaveChangesAsync();
        await audit.LogAsync(username, "Check Audit Scan Discrepancies", "AuditScanPeriod", periodId.ToString());

        return new DiscrepancyCheckResult
        {
            Period = period,
            DistinctScannedCount = distinctScanned.Count,
            TotalScanBatches = scans.Count,
            Concerns = concerns
        };
    }

    public async Task<List<AuditDiscrepancy>> GetDiscrepanciesAsync(int periodId)
        => await db.AuditDiscrepancies.AsQueryable()
            .Where(d => d.PeriodId == periodId)
            .OrderBy(d => d.Type)
            .ThenBy(d => d.RfidCode)
            .ToListAsync();
}

public class DiscrepancyCheckResult
{
    public AuditScanPeriod Period { get; set; } = null!;
    public int DistinctScannedCount { get; set; }
    public int TotalScanBatches { get; set; }
    public List<AuditDiscrepancy> Concerns { get; set; } = [];
}
