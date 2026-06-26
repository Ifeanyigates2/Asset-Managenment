using FrislEams.Web.Configuration;
using FrislEams.Web.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FrislEams.Web.Services;

public sealed class MongoAssetSyncService(
    IMongoClient mongoClient,
    IOptions<MongoDbOptions> options,
    ILogger<MongoAssetSyncService> logger)
{
    private readonly MongoDbOptions mongoOptions = options.Value;

    public async Task SaveAssetAsync(Asset asset, string rfidCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(mongoOptions.AssetCollectionName);

            await EnsureIndexesAsync(collection, cancellationToken);

            var document = new BsonDocument
            {
                { "documentType", "RegisteredAsset" },
                { "sqliteAssetId", asset.Id },
                { "tagCode", asset.TagCode },
                { "assetName", asset.AssetName },
                { "description", asset.Description },
                { "assetCategoryId", asset.AssetCategoryId },
                { "purchaseDate", asset.PurchaseDate.HasValue ? asset.PurchaseDate.Value : BsonNull.Value },
                { "purchaseCost", asset.PurchaseCost.HasValue ? asset.PurchaseCost.Value : BsonNull.Value },
                { "glCode", ToBsonValue(asset.GlCode) },
                { "stateOfPurchase", asset.StateOfPurchase },
                { "supplierId", asset.SupplierId.HasValue ? asset.SupplierId.Value : BsonNull.Value },
                { "serialNumber", ToBsonValue(asset.SerialNumber) },
                { "tagNumber", ToBsonValue(asset.TagNumber) },
                { "modelNumber", ToBsonValue(asset.ModelNumber) },
                { "brand", ToBsonValue(asset.Brand) },
                { "warrantyExpiryDate", asset.WarrantyExpiryDate.HasValue ? asset.WarrantyExpiryDate.Value : BsonNull.Value },
                { "expectedServiceYears", asset.ExpectedServiceYears.HasValue ? asset.ExpectedServiceYears.Value : BsonNull.Value },
                { "currentCondition", asset.CurrentCondition },
                { "currentStatus", asset.CurrentStatus.ToString() },
                { "currentLocationId", asset.CurrentLocationId.HasValue ? asset.CurrentLocationId.Value : BsonNull.Value },
                { "currentDepartmentId", asset.CurrentDepartmentId.HasValue ? asset.CurrentDepartmentId.Value : BsonNull.Value },
                { "currentCustodianId", asset.CurrentCustodianId.HasValue ? asset.CurrentCustodianId.Value : BsonNull.Value },
                { "rfidCode", rfidCode },
                { "notes", ToBsonValue(asset.Notes) },
                { "createdAt", asset.CreatedAt },
                { "updatedAt", asset.UpdatedAt },
                { "syncedAt", DateTime.UtcNow }
            };

            var filter = Builders<BsonDocument>.Filter.Eq("tagCode", asset.TagCode);
            await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB asset sync skipped for {TagCode}.", asset.TagCode);
        }
    }

    private static async Task EnsureIndexesAsync(IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken)
    {
        await DropLegacyUniqueIndexAsync(collection, "tagCode_1", cancellationToken);
        await DropLegacyUniqueIndexAsync(collection, "rfidCode_1", cancellationToken);

        var indexes = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("tagCode"),
                new CreateIndexOptions<BsonDocument>
                {
                    Name = "asset_tagCode_unique_registered",
                    Unique = true,
                    PartialFilterExpression = new BsonDocument
                    {
                        { "documentType", "RegisteredAsset" },
                        { "tagCode", new BsonDocument("$type", "string") }
                    }
                }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("rfidCode"),
                new CreateIndexOptions<BsonDocument>
                {
                    Name = "asset_rfidCode_unique_registered",
                    Unique = true,
                    PartialFilterExpression = new BsonDocument
                    {
                        { "documentType", "RegisteredAsset" },
                        { "rfidCode", new BsonDocument("$type", "string") }
                    }
                }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("documentType")),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("createdAt"))
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private static async Task DropLegacyUniqueIndexAsync(IMongoCollection<BsonDocument> collection, string indexName, CancellationToken cancellationToken)
    {
        try
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.CodeName is "IndexNotFound" or "NamespaceNotFound")
        {
        }
    }

    private static BsonValue ToBsonValue(string? value) => string.IsNullOrWhiteSpace(value) ? BsonNull.Value : value.Trim();
}
