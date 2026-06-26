using FrislEams.Web.Data;

namespace FrislEams.Web.Services;

public class TagCodeGenerator(AppDbContext db)
{
    public string Generate(int categoryId, int? departmentId)
    {
        var categoryCode = db.AssetCategories.AsQueryable().Where(c => c.Id == categoryId).Select(c => c.Code).FirstOrDefault() ?? "CAT";
        var departmentCode = departmentId.HasValue
            ? db.Departments.AsQueryable().Where(d => d.Id == departmentId.Value).Select(d => d.Code).FirstOrDefault() ?? "GEN"
            : "GEN";

        var year = DateTime.UtcNow.Year;
        var prefix = $"FRISL-{year}-{categoryCode}-{departmentCode}";
        var next = db.Assets.AsQueryable().Count(a => a.TagCode.StartsWith(prefix)) + 1;
        return $"{prefix}-{next:D5}";
    }
}
