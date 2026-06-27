using FrislEams.Web.Configuration;
using FrislEams.Web.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace FrislEams.Web.Data;

public sealed class AppDbContext
{
    private readonly IMongoDatabase database;
    private readonly MongoIdGenerator idGenerator;

    public AppDbContext(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var mongoOptions = options.Value;
        database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        idGenerator = new MongoIdGenerator(database);

        Assets = CreateSet<Asset>("assets");
        AssetCategories = CreateSet<AssetCategory>("assetCategories");
        Departments = CreateSet<Department>("departments");
        Locations = CreateSet<Location>("locations");
        Suppliers = CreateSet<Supplier>("suppliers");
        RepairContractors = CreateSet<RepairContractor>("repairContractors");
        Staff = CreateSet<Staff>("staff");
        RfidTags = CreateSet<RfidTag>("rfidTags");
        AssetTypes = CreateSet<AssetType>("assetTypes");
        Manufacturers = CreateSet<Manufacturer>("manufacturers");
        AssetTransfers = CreateSet<AssetTransfer>("assetTransfers");
        StockVerificationSessions = CreateSet<StockVerificationSession>("stockVerificationSessions");
        StockVerificationScans = CreateSet<StockVerificationScan>("stockVerificationScans");
        SystemAuditLogs = CreateSet<SystemAuditLog>("systemAuditLogs");
        AssetAssignments = CreateSet<AssetAssignment>("assetAssignments");
        AssetStatusHistories = CreateSet<AssetStatusHistory>("assetStatusHistories");
        AssetRequests = CreateSet<AssetRequest>("assetRequests");
        RepairRequests = CreateSet<RepairRequest>("repairRequests");
        LoanRequests = CreateSet<LoanRequest>("loanRequests");
        ExitGrants = CreateSet<ExitGrant>("exitGrants");
        RfidEvents = CreateSet<RfidEvent>("rfidEvents");
        AuditSessions = CreateSet<AuditSession>("auditSessions");
        AuditResults = CreateSet<AuditResult>("auditResults");
        AuditScanPeriods = CreateSet<AuditScanPeriod>("auditScanPeriods");
        AuditTemporaryScans = CreateSet<AuditTemporaryScan>("auditTemporaryScans");
        AuditTemporaryScanItems = CreateSet<AuditTemporaryScanItem>("auditTemporaryScanItems");
        AuditDiscrepancies = CreateSet<AuditDiscrepancy>("auditDiscrepancies");
        Notifications = CreateSet<Notification>("notifications");
        ProcurementRecords = CreateSet<ProcurementRecord>("procurementRecords");
        IntegrationEventLogs = CreateSet<IntegrationEventLog>("integrationEventLogs");
        UserAccounts = CreateSet<UserAccount>(mongoOptions.UsersCollectionName);
    }

    /// <summary>MongoDB <c>users</c> collection (login accounts).</summary>
    public MongoEntitySet<UserAccount> Users => UserAccounts;

    public MongoEntitySet<Asset> Assets { get; }
    public MongoEntitySet<AssetCategory> AssetCategories { get; }
    public MongoEntitySet<Department> Departments { get; }
    public MongoEntitySet<Location> Locations { get; }
    public MongoEntitySet<Supplier> Suppliers { get; }
    public MongoEntitySet<RepairContractor> RepairContractors { get; }
    public MongoEntitySet<Staff> Staff { get; }
    public MongoEntitySet<RfidTag> RfidTags { get; }
    public MongoEntitySet<AssetType> AssetTypes { get; }
    public MongoEntitySet<Manufacturer> Manufacturers { get; }
    public MongoEntitySet<AssetTransfer> AssetTransfers { get; }
    public MongoEntitySet<StockVerificationSession> StockVerificationSessions { get; }
    public MongoEntitySet<StockVerificationScan> StockVerificationScans { get; }
    public MongoEntitySet<SystemAuditLog> SystemAuditLogs { get; }
    public MongoEntitySet<AssetAssignment> AssetAssignments { get; }
    public MongoEntitySet<AssetStatusHistory> AssetStatusHistories { get; }
    public MongoEntitySet<AssetRequest> AssetRequests { get; }
    public MongoEntitySet<RepairRequest> RepairRequests { get; }
    public MongoEntitySet<LoanRequest> LoanRequests { get; }
    public MongoEntitySet<ExitGrant> ExitGrants { get; }
    public MongoEntitySet<RfidEvent> RfidEvents { get; }
    public MongoEntitySet<AuditSession> AuditSessions { get; }
    public MongoEntitySet<AuditResult> AuditResults { get; }
    public MongoEntitySet<AuditScanPeriod> AuditScanPeriods { get; }
    public MongoEntitySet<AuditTemporaryScan> AuditTemporaryScans { get; }
    public MongoEntitySet<AuditTemporaryScanItem> AuditTemporaryScanItems { get; }
    public MongoEntitySet<AuditDiscrepancy> AuditDiscrepancies { get; }
    public MongoEntitySet<Notification> Notifications { get; }
    public MongoEntitySet<ProcurementRecord> ProcurementRecords { get; }
    public MongoEntitySet<IntegrationEventLog> IntegrationEventLogs { get; }
    public MongoEntitySet<UserAccount> UserAccounts { get; }

    public IMongoDatabase Database => database;

    public Task<int> NextIdAsync(string sequenceName, CancellationToken cancellationToken = default)
        => idGenerator.NextAsync(sequenceName, cancellationToken);

    public Task<long> CountUsersAsync(CancellationToken cancellationToken = default)
        => UserAccounts.Collection.CountDocumentsAsync(FilterDefinition<UserAccount>.Empty, cancellationToken: cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveAllAsync(cancellationToken);
    }

    public void SaveChanges() => SaveAllAsync(CancellationToken.None).GetAwaiter().GetResult();

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await CreateIndexSafeAsync(
            Assets.Collection,
            new CreateIndexModel<Asset>(
                Builders<Asset>.IndexKeys.Ascending(a => a.TagCode),
                new CreateIndexOptions { Unique = true, Name = "TagCode_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            Assets.Collection,
            new CreateIndexModel<Asset>(
                Builders<Asset>.IndexKeys.Ascending(a => a.TagNumber),
                new CreateIndexOptions { Name = "TagNumber_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            RfidTags.Collection,
            new CreateIndexModel<RfidTag>(
                Builders<RfidTag>.IndexKeys.Ascending(r => r.RfidCode),
                new CreateIndexOptions { Unique = true, Name = "RfidCode_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            RfidTags.Collection,
            new CreateIndexModel<RfidTag>(
                Builders<RfidTag>.IndexKeys.Ascending(r => r.AssetId),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "AssetId_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            UserAccounts.Collection,
            new CreateIndexModel<UserAccount>(
                Builders<UserAccount>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "Username_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            Staff.Collection,
            new CreateIndexModel<Staff>(
                Builders<Staff>.IndexKeys.Ascending(s => s.StaffId),
                new CreateIndexOptions { Name = "StaffId_1" }),
            cancellationToken);
        await CreateIndexSafeAsync(
            AuditResults.Collection,
            new CreateIndexModel<AuditResult>(
                Builders<AuditResult>.IndexKeys.Combine(
                    Builders<AuditResult>.IndexKeys.Ascending(a => a.AuditSessionId),
                    Builders<AuditResult>.IndexKeys.Ascending(a => a.AssetId)),
                new CreateIndexOptions { Unique = true, Name = "AuditSessionId_1_AssetId_1" }),
            cancellationToken);
    }

    private static async Task CreateIndexSafeAsync<T>(
        IMongoCollection<T> collection,
        CreateIndexModel<T> model,
        CancellationToken cancellationToken)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;
        var indexName = model.Options?.Name ?? "unknown";

        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (IsIndexOptionsConflict(ex))
        {
            Console.WriteLine(
                $"FRISL EAMS startup: replacing conflicting index '{indexName}' on '{collectionName}' ({ex.Message})");
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (IsIndexAlreadyExists(ex))
        {
            Console.WriteLine(
                $"FRISL EAMS startup: index '{indexName}' already exists on '{collectionName}', continuing.");
        }
    }

    private static bool IsIndexOptionsConflict(MongoCommandException ex)
        => ex.Code == 85
           || string.Equals(ex.CodeName, "IndexOptionsConflict", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("has the same name as the requested index", StringComparison.OrdinalIgnoreCase);

    private static bool IsIndexAlreadyExists(MongoCommandException ex)
        => ex.Code == 68
           || string.Equals(ex.CodeName, "IndexAlreadyExists", StringComparison.OrdinalIgnoreCase);

    public static void RegisterClassMaps()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Asset)))
        {
            return;
        }

        RegisterMap<Asset>();
        RegisterMap<AssetCategory>();
        RegisterMap<Department>();
        RegisterMap<Location>();
        RegisterMap<Supplier>();
        RegisterMap<RepairContractor>();
        RegisterMap<Staff>();
        RegisterMap<RfidTag>();
        RegisterMap<AssetType>();
        RegisterMap<Manufacturer>();
        RegisterMap<AssetTransfer>();
        RegisterMap<StockVerificationSession>();
        RegisterMap<StockVerificationScan>();
        RegisterMap<SystemAuditLog>();
        RegisterMap<AssetAssignment>();
        RegisterMap<AssetStatusHistory>();
        RegisterMap<AssetRequest>();
        RegisterMap<RepairRequest>();
        RegisterMap<LoanRequest>();
        RegisterMap<ExitGrant>();
        RegisterMap<RfidEvent>();
        RegisterMap<AuditSession>();
        RegisterMap<AuditResult>();
        RegisterMap<AuditScanPeriod>();
        RegisterMap<AuditTemporaryScan>();
        RegisterMap<AuditTemporaryScanItem>();
        RegisterMap<AuditDiscrepancy>();
        RegisterMap<Notification>();
        RegisterMap<ProcurementRecord>();
        RegisterMap<IntegrationEventLog>();
        RegisterMap<UserAccount>();
    }

    private MongoEntitySet<T> CreateSet<T>(string collectionName) where T : class
        => new(database.GetCollection<T>(collectionName), idGenerator, collectionName);

    private async Task SaveAllAsync(CancellationToken cancellationToken)
    {
        await Assets.SaveChangesAsync(cancellationToken);
        await AssetCategories.SaveChangesAsync(cancellationToken);
        await Departments.SaveChangesAsync(cancellationToken);
        await Locations.SaveChangesAsync(cancellationToken);
        await Suppliers.SaveChangesAsync(cancellationToken);
        await RepairContractors.SaveChangesAsync(cancellationToken);
        await Staff.SaveChangesAsync(cancellationToken);
        await RfidTags.SaveChangesAsync(cancellationToken);
        await AssetTypes.SaveChangesAsync(cancellationToken);
        await Manufacturers.SaveChangesAsync(cancellationToken);
        await AssetTransfers.SaveChangesAsync(cancellationToken);
        await StockVerificationSessions.SaveChangesAsync(cancellationToken);
        await StockVerificationScans.SaveChangesAsync(cancellationToken);
        await SystemAuditLogs.SaveChangesAsync(cancellationToken);
        await AssetAssignments.SaveChangesAsync(cancellationToken);
        await AssetStatusHistories.SaveChangesAsync(cancellationToken);
        await AssetRequests.SaveChangesAsync(cancellationToken);
        await RepairRequests.SaveChangesAsync(cancellationToken);
        await LoanRequests.SaveChangesAsync(cancellationToken);
        await ExitGrants.SaveChangesAsync(cancellationToken);
        await RfidEvents.SaveChangesAsync(cancellationToken);
        await AuditSessions.SaveChangesAsync(cancellationToken);
        await AuditResults.SaveChangesAsync(cancellationToken);
        await AuditScanPeriods.SaveChangesAsync(cancellationToken);
        await AuditTemporaryScans.SaveChangesAsync(cancellationToken);
        await AuditTemporaryScanItems.SaveChangesAsync(cancellationToken);
        await AuditDiscrepancies.SaveChangesAsync(cancellationToken);
        await Notifications.SaveChangesAsync(cancellationToken);
        await ProcurementRecords.SaveChangesAsync(cancellationToken);
        await IntegrationEventLogs.SaveChangesAsync(cancellationToken);
        await UserAccounts.SaveChangesAsync(cancellationToken);
    }

    private static void RegisterMap<T>()
    {
        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(typeof(T).GetProperty("Id")!);
            cm.SetIgnoreExtraElements(true);
        });
    }
}
