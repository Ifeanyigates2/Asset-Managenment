namespace FrislEams.Web.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "frisl_asset_management";
    public string UsersCollectionName { get; set; } = "users";
    public string AssetCollectionName { get; set; } = "ASSET";
    public string VendorCollectionName { get; set; } = "vendors";
    public string BatchCollectionName { get; set; } = "asset_import_batches";
    public string RowCollectionName { get; set; } = "asset_import_rows";
}
