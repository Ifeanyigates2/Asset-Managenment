using FrislEams.Web.Configuration;
using FrislEams.Web.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FrislEams.Web.Services;

public sealed class MongoVendorService(
    IMongoClient mongoClient,
    IOptions<MongoDbOptions> options,
    ILogger<MongoVendorService> logger)
{
    private readonly MongoDbOptions mongoOptions = options.Value;

    public async Task<List<VendorVm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            await EnsureIndexesAsync(collection, cancellationToken);

            var documents = await collection
                .Find(Builders<BsonDocument>.Filter.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending("name"))
                .ToListAsync(cancellationToken);

            return documents.Select(ToVendorVm).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB vendor list unavailable; returning an empty list.");
            return [];
        }
    }

    public async Task<VendorVm?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        try
        {
            var document = await GetCollection()
                .Find(Builders<BsonDocument>.Filter.Eq("_id", objectId))
                .FirstOrDefaultAsync(cancellationToken);

            return document is null ? null : ToVendorVm(document);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB vendor lookup failed for {VendorId}.", id);
            return null;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, string? excludingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);
        var filter = Builders<BsonDocument>.Filter.Eq("code", normalizedCode);

        if (ObjectId.TryParse(excludingId, out var objectId))
        {
            filter &= Builders<BsonDocument>.Filter.Ne("_id", objectId);
        }

        try
        {
            return await GetCollection().Find(filter).AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB vendor code check failed for {VendorCode}.", normalizedCode);
            return false;
        }
    }

    public async Task CreateAsync(VendorVm vendor, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            await EnsureIndexesAsync(collection, cancellationToken);

            var now = DateTime.UtcNow;
            vendor.Code = NormalizeCode(vendor.Code);
            vendor.RegisteredAt = now;
            vendor.UpdatedAt = now;
            vendor.IsActive = true;

            await collection.InsertOneAsync(ToDocument(vendor), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB vendor create skipped for {VendorCode}.", vendor.Code);
        }
    }

    public async Task UpdateAsync(VendorVm vendor, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(vendor.Id, out var objectId))
        {
            throw new InvalidOperationException("Invalid vendor id.");
        }

        var update = Builders<BsonDocument>.Update
            .Set("name", vendor.Name.Trim())
            .Set("code", NormalizeCode(vendor.Code))
            .Set("contactPerson", Normalize(vendor.ContactPerson))
            .Set("phone", Normalize(vendor.Phone))
            .Set("email", Normalize(vendor.Email))
            .Set("address", Normalize(vendor.Address))
            .Set("category", Normalize(vendor.Category))
            .Set("notes", Normalize(vendor.Notes))
            .Set("isActive", vendor.IsActive)
            .Set("updatedAt", DateTime.UtcNow);

        try
        {
            await GetCollection().UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", objectId),
                update,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB vendor update skipped for {VendorId}.", vendor.Id);
        }
    }

    public async Task<VendorVm?> ToggleActiveAsync(string id, CancellationToken cancellationToken = default)
    {
        var vendor = await GetByIdAsync(id, cancellationToken);
        if (vendor is null)
        {
            return null;
        }

        vendor.IsActive = !vendor.IsActive;
        await UpdateAsync(vendor, cancellationToken);
        return vendor;
    }

    private IMongoCollection<BsonDocument> GetCollection()
    {
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        return database.GetCollection<BsonDocument>(mongoOptions.VendorCollectionName);
    }

    private static async Task EnsureIndexesAsync(IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("code"),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("name")),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("isActive"))
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private static BsonDocument ToDocument(VendorVm vendor) => new()
    {
        { "name", vendor.Name.Trim() },
        { "code", NormalizeCode(vendor.Code) },
        { "contactPerson", Normalize(vendor.ContactPerson) },
        { "phone", Normalize(vendor.Phone) },
        { "email", Normalize(vendor.Email) },
        { "address", Normalize(vendor.Address) },
        { "category", Normalize(vendor.Category) },
        { "notes", Normalize(vendor.Notes) },
        { "isActive", vendor.IsActive },
        { "registeredAt", vendor.RegisteredAt },
        { "updatedAt", vendor.UpdatedAt }
    };

    private static VendorVm ToVendorVm(BsonDocument document) => new()
    {
        Id = document.GetValue("_id", BsonNull.Value).ToString(),
        Name = GetString(document, "name"),
        Code = GetString(document, "code"),
        ContactPerson = GetString(document, "contactPerson"),
        Phone = GetString(document, "phone"),
        Email = GetString(document, "email"),
        Address = GetString(document, "address"),
        Category = GetString(document, "category"),
        Notes = GetString(document, "notes"),
        IsActive = document.GetValue("isActive", true).ToBoolean(),
        RegisteredAt = GetDateTime(document, "registeredAt"),
        UpdatedAt = GetDateTime(document, "updatedAt")
    };

    private static string GetString(BsonDocument document, string field)
    {
        var value = document.GetValue(field, BsonNull.Value);
        return value.IsBsonNull ? string.Empty : value.ToString() ?? string.Empty;
    }

    private static DateTime GetDateTime(BsonDocument document, string field)
    {
        var value = document.GetValue(field, BsonNull.Value);
        return value.IsValidDateTime ? value.ToUniversalTime() : DateTime.UtcNow;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static BsonValue Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? BsonNull.Value : value.Trim();
}
