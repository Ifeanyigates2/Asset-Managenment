using System.Text;
using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class ReportingService(AppDbContext db)
{
    public async Task<List<Asset>> GetAssetsReportAsync(ReportFilterVm filter)
    {
        var query = db.Assets.AsQueryable();

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.PurchaseDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.PurchaseDate <= filter.ToDate.Value);
        if (filter.DepartmentId.HasValue)
            query = query.Where(a => a.CurrentDepartmentId == filter.DepartmentId.Value);
        if (filter.CategoryId.HasValue)
            query = query.Where(a => a.AssetCategoryId == filter.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<AssetStatus>(filter.Status, out var status))
            query = query.Where(a => a.CurrentStatus == status);
        if (!string.IsNullOrWhiteSpace(filter.Condition))
            query = query.Where(a => a.CurrentCondition == filter.Condition);

        var assets = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        return assets;
    }

    public string BuildAssetsCsv(List<Asset> assets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TagCode,AssetName,Category,Brand,SerialNumber,Department,Location,Custodian,Status,Condition,PurchaseDate,PurchaseCost,WarrantyExpiry");
        foreach (var a in assets)
        {
            sb.AppendLine(string.Join(",",
                Csv(a.TagCode), Csv(a.AssetName), Csv(a.AssetCategory?.Name), Csv(a.Brand),
                Csv(a.SerialNumber), Csv(a.CurrentDepartment?.Name), Csv(a.CurrentLocation?.Name),
                Csv(a.CurrentCustodian?.FullName), Csv(a.CurrentStatus.ToString()), Csv(a.CurrentCondition),
                Csv(a.PurchaseDate?.ToString("yyyy-MM-dd")), Csv(a.PurchaseCost?.ToString("0.00")),
                Csv(a.WarrantyExpiryDate?.ToString("yyyy-MM-dd"))
            ));
        }
        return sb.ToString();
    }

    public async Task<Dictionary<string, int>> GetAssetCountByCategoryAsync()
    {
        var assets = await db.Assets.AsQueryable().ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        return assets
            .Where(a => a.AssetCategory != null)
            .GroupBy(a => a.AssetCategory!.Name)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<DepreciationRowVm>> GetDepreciationReportAsync()
    {
        var assets = await db.Assets.AsQueryable()
            .Where(a => a.PurchaseDate.HasValue && a.PurchaseCost.HasValue)
            .ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);
        assets = assets.Where(a => a.AssetCategory != null).ToList();

        var today = DateTime.UtcNow.Date;
        var report = new List<DepreciationRowVm>(assets.Count);

        foreach (var a in assets)
        {
            var usefulLife = Math.Max(1, a.AssetCategory!.UsefulLifeYears);
            var annualDep = a.PurchaseCost!.Value / usefulLife;
            var ageYears = Math.Max(0, (today - a.PurchaseDate!.Value.Date).Days / 365m);
            var ageYearsInt = (int)ageYears;
            var accumulated = Math.Min(a.PurchaseCost.Value, annualDep * ageYears);
            var nbv = Math.Max(0, a.PurchaseCost.Value - accumulated);
            var pct = a.PurchaseCost.Value > 0 ? (accumulated / a.PurchaseCost.Value * 100m) : 0m;

            report.Add(new DepreciationRowVm
            {
                AssetId = a.Id,
                TagCode = a.TagCode,
                AssetName = a.AssetName,
                Category = a.AssetCategory.Name,
                PurchaseCost = a.PurchaseCost.Value,
                UsefulLifeYears = usefulLife,
                AnnualDepreciation = Decimal.Round(annualDep, 2),
                AccumulatedDepreciation = Decimal.Round(accumulated, 2),
                NetBookValue = Decimal.Round(nbv, 2),
                AgeYears = ageYearsInt,
                DepreciationPct = pct.ToString("0.1") + "%"
            });
        }

        return report.OrderBy(r => r.TagCode).ToList();
    }

    public string BuildDepreciationCsv(List<DepreciationRowVm> report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AssetId,TagCode,AssetName,Category,PurchaseCost,UsefulLifeYears,AgeYears,AnnualDepreciation,AccumulatedDepreciation,NetBookValue,DepreciationPct");
        foreach (var row in report)
        {
            sb.AppendLine(string.Join(",",
                row.AssetId, Csv(row.TagCode), Csv(row.AssetName), Csv(row.Category),
                row.PurchaseCost.ToString("0.00"), row.UsefulLifeYears, row.AgeYears,
                row.AnnualDepreciation.ToString("0.00"), row.AccumulatedDepreciation.ToString("0.00"),
                row.NetBookValue.ToString("0.00"), Csv(row.DepreciationPct)
            ));
        }
        return sb.ToString();
    }

    public async Task<List<AgingRowVm>> GetAgingReportAsync()
    {
        var assets = await db.Assets.AsQueryable().ToListAsync();
        MongoHydrator.HydrateAssets(assets, db);

        var today = DateTime.UtcNow.Date;
        var report = new List<AgingRowVm>(assets.Count);

        foreach (var a in assets)
        {
            var ageMonths = a.PurchaseDate.HasValue
                ? (int)((today - a.PurchaseDate.Value.Date).Days / 30.44)
                : 0;
            var ageYears = ageMonths / 12;
            var usefulLife = a.AssetCategory?.UsefulLifeYears ?? 5;

            string ageBand;
            if (ageYears < 3) ageBand = "0-3 Years";
            else if (ageYears < 5) ageBand = "3-5 Years";
            else if (ageYears < 10) ageBand = "5-10 Years";
            else ageBand = "10+ Years";

            report.Add(new AgingRowVm
            {
                AssetId = a.Id,
                TagCode = a.TagCode,
                AssetName = a.AssetName,
                Category = a.AssetCategory?.Name ?? "-",
                Department = a.CurrentDepartment?.Name ?? "Unassigned",
                Location = a.CurrentLocation?.Name ?? "-",
                Status = a.CurrentStatus.ToString(),
                Condition = a.CurrentCondition,
                PurchaseDate = a.PurchaseDate,
                AgeMonths = ageMonths,
                AgeYears = ageYears,
                UsefulLifeYears = usefulLife,
                AgeBand = ageBand,
                PurchaseCost = a.PurchaseCost,
                WarrantyExpiry = a.WarrantyExpiryDate,
                WarrantyExpired = a.WarrantyExpiryDate.HasValue && a.WarrantyExpiryDate.Value < DateTime.UtcNow
            });
        }

        return report.OrderByDescending(r => r.AgeMonths).ToList();
    }

    public string BuildAgingCsv(List<AgingRowVm> report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TagCode,AssetName,Category,Department,Location,Status,Condition,PurchaseDate,AgeYears,AgeMonths,AgeBand,UsefulLifeYears,PurchaseCost,WarrantyExpiry,WarrantyExpired");
        foreach (var r in report)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.TagCode), Csv(r.AssetName), Csv(r.Category), Csv(r.Department), Csv(r.Location),
                Csv(r.Status), Csv(r.Condition), Csv(r.PurchaseDate?.ToString("yyyy-MM-dd")),
                r.AgeYears, r.AgeMonths, Csv(r.AgeBand), r.UsefulLifeYears,
                Csv(r.PurchaseCost?.ToString("0.00")), Csv(r.WarrantyExpiry?.ToString("yyyy-MM-dd")),
                r.WarrantyExpired ? "Yes" : "No"
            ));
        }
        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
