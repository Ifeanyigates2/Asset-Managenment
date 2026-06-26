using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FrislEams.Web.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FrislEams.Web.Services;

public sealed class MongoWorkbookImportService(
    IMongoClient mongoClient,
    IOptions<MongoDbOptions> options,
    ILogger<MongoWorkbookImportService> logger)
{
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private readonly MongoDbOptions mongoOptions = options.Value;

    public async Task<MongoWorkbookImportSummary> ImportAsync(string workbookPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workbookPath))
        {
            throw new InvalidOperationException("Workbook path is required.");
        }

        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("Workbook file was not found.", workbookPath);
        }

        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        var assetCollection = database.GetCollection<BsonDocument>(mongoOptions.AssetCollectionName);

        var sourceFileName = Path.GetFileName(workbookPath);
        await assetCollection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Eq("documentType", "ImportedAsset")
            & Builders<BsonDocument>.Filter.Eq("sourceFileName", sourceFileName),
            cancellationToken);

        await EnsureIndexesAsync(assetCollection, cancellationToken);

        var workbook = ReadWorkbook(workbookPath);
        var batchId = Guid.NewGuid().ToString("N");
        var importedAt = DateTime.UtcNow;
        var sheetNames = workbook.Select(s => s.Name).ToList();
        var documents = new List<BsonDocument>();

        foreach (var sheet in workbook)
        {
            var headerIndex = FindHeaderRowIndex(sheet.Rows);
            var headerRow = headerIndex >= 0 ? sheet.Rows[headerIndex] : null;
            var titleRows = headerIndex > 0 ? sheet.Rows.Take(headerIndex).Where(r => r.Cells.Any(c => !string.IsNullOrWhiteSpace(c))).ToList() : [];
            var locationLabel = titleRows.FirstOrDefault()?.Cells.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? sheet.Name;
            var sectionLabel = titleRows.Skip(1).Select(r => r.Cells.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            var headers = headerRow?.Cells.Select(NormalizeHeaderCell).ToList() ?? [];
            var dataRows = headerIndex >= 0 ? sheet.Rows.Skip(headerIndex + 1) : sheet.Rows;

            foreach (var row in dataRows.Where(IsImportableRow))
            {
                var extracted = ExtractFields(headers, row.Cells);
                documents.Add(new BsonDocument
                {
                    // MongoDB adds _id first; keeping itemName as the first application field places it directly after _id.
                    { "itemName", ToBsonValue(extracted.ItemName) },
                    { "documentType", "ImportedAsset" },
                    { "batchId", batchId },
                    { "sourceFileName", sourceFileName },
                    { "sourcePath", workbookPath },
                    { "importSummary", new BsonDocument
                        {
                            { "databaseName", mongoOptions.DatabaseName },
                            { "collectionName", mongoOptions.AssetCollectionName },
                            { "sheetCount", workbook.Count },
                            { "sheetNames", new BsonArray(sheetNames) }
                        }
                    },
                    { "sheetName", sheet.Name },
                    { "locationLabel", ToBsonValue(locationLabel) },
                    { "sectionLabel", ToBsonValue(sectionLabel) },
                    { "rowNumber", row.RowNumber },
                    { "headers", new BsonArray(headers.Select(ToBsonValue)) },
                    { "rawCells", new BsonArray(row.Cells.Select(ToBsonValue)) },
                    { "sequenceNumber", ToBsonValue(extracted.SequenceNumber) },
                    { "quantity", extracted.Quantity.HasValue ? extracted.Quantity.Value : BsonNull.Value },
                    { "quantityText", ToBsonValue(extracted.QuantityText) },
                    { "moveableCount", extracted.MoveableCount.HasValue ? extracted.MoveableCount.Value : BsonNull.Value },
                    { "stationaryCount", extracted.StationaryCount.HasValue ? extracted.StationaryCount.Value : BsonNull.Value },
                    { "brand", ToBsonValue(extracted.Brand) },
                    { "serialNumber", ToBsonValue(extracted.SerialNumber) },
                    { "status", ToBsonValue(extracted.Status) },
                    { "importedAt", importedAt }
                });
            }
        }

        if (documents.Count > 0)
        {
            await assetCollection.InsertManyAsync(documents, cancellationToken: cancellationToken);
        }

        await DropLegacyImportCollectionsAsync(database, cancellationToken);
        logger.LogInformation("Imported {RowCount} workbook rows from {WorkbookPath} into MongoDB collection {CollectionName}.", documents.Count, workbookPath, mongoOptions.AssetCollectionName);

        return new MongoWorkbookImportSummary(batchId, mongoOptions.DatabaseName, workbook.Count, documents.Count);
    }

    private static async Task EnsureIndexesAsync(
        IMongoCollection<BsonDocument> assetCollection,
        CancellationToken cancellationToken)
    {
        await DropLegacyUniqueIndexAsync(assetCollection, "tagCode_1", cancellationToken);
        await DropLegacyUniqueIndexAsync(assetCollection, "rfidCode_1", cancellationToken);

        var assetIndexes = new[]
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
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("documentType")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("sourceFileName").Ascending("sheetName").Ascending("rowNumber")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("itemName")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("locationLabel")),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("importedAt"))
        };

        await assetCollection.Indexes.CreateManyAsync(assetIndexes, cancellationToken);
    }

    private async Task DropLegacyImportCollectionsAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        await DropCollectionIfExistsAsync(database, mongoOptions.BatchCollectionName, cancellationToken);
        await DropCollectionIfExistsAsync(database, mongoOptions.RowCollectionName, cancellationToken);
    }

    private static async Task DropCollectionIfExistsAsync(IMongoDatabase database, string collectionName, CancellationToken cancellationToken)
    {
        try
        {
            await database.DropCollectionAsync(collectionName, cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.CodeName is "NamespaceNotFound")
        {
        }
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

    private static List<WorkbookSheet> ReadWorkbook(string workbookPath)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var workbookDocument = XDocument.Load(archive.GetEntry("xl/workbook.xml")!.Open());
        var relationshipsDocument = XDocument.Load(archive.GetEntry("xl/_rels/workbook.xml.rels")!.Open());

        var relationshipMap = relationshipsDocument.Root!
            .Elements()
            .Where(e => e.Attribute("Id") != null && e.Attribute("Target") != null)
            .ToDictionary(e => e.Attribute("Id")!.Value, e => e.Attribute("Target")!.Value);

        var sheets = new List<WorkbookSheet>();
        foreach (var sheetElement in workbookDocument.Descendants(SpreadsheetNs + "sheet"))
        {
            var name = sheetElement.Attribute("name")?.Value ?? "Sheet";
            var relationshipId = sheetElement.Attribute(RelationshipNs + "id")?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId) || !relationshipMap.TryGetValue(relationshipId, out var target))
            {
                continue;
            }

            var sheetEntry = archive.GetEntry($"xl/{target.Replace("\\", "/")}");
            if (sheetEntry is null)
            {
                continue;
            }

            var sheetDocument = XDocument.Load(sheetEntry.Open());
            var rows = new List<WorkbookRow>();
            foreach (var rowElement in sheetDocument.Descendants(SpreadsheetNs + "row"))
            {
                var rowNumber = (int?)rowElement.Attribute("r") ?? rows.Count + 1;
                var cells = ReadRowCells(rowElement, sharedStrings);
                rows.Add(new WorkbookRow(rowNumber, cells));
            }

            sheets.Add(new WorkbookSheet(name, rows));
        }

        return sheets;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        var document = XDocument.Load(entry.Open());
        return document.Descendants(SpreadsheetNs + "si")
            .Select(si => string.Concat(si.Descendants(SpreadsheetNs + "t").Select(t => t.Value)))
            .ToList();
    }

    private static List<string> ReadRowCells(XElement rowElement, List<string> sharedStrings)
    {
        var cells = new List<string>();
        foreach (var cellElement in rowElement.Elements(SpreadsheetNs + "c"))
        {
            var reference = cellElement.Attribute("r")?.Value;
            var columnIndex = GetColumnIndex(reference);
            while (cells.Count < columnIndex)
            {
                cells.Add(string.Empty);
            }

            cells.Add(ReadCellValue(cellElement, sharedStrings));
        }

        return cells;
    }

    private static string ReadCellValue(XElement cellElement, List<string> sharedStrings)
    {
        var cellType = cellElement.Attribute("t")?.Value;
        var rawValue = cellElement.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
        if (cellType == "s" && int.TryParse(rawValue, out var index) && index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index].Trim();
        }

        return rawValue.Trim();
    }

    private static int GetColumnIndex(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return 0;
        }

        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        var index = 0;
        foreach (var letter in letters)
        {
            index = (index * 26) + (char.ToUpperInvariant(letter) - 'A' + 1);
        }

        return Math.Max(index - 1, 0);
    }

    private static int FindHeaderRowIndex(IReadOnlyList<WorkbookRow> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var normalized = rows[i].Cells.Select(NormalizeHeader).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            if (normalized.Any(v => v is "item" or "items" or "category name"))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsImportableRow(WorkbookRow row)
    {
        var nonEmptyCells = row.Cells.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (nonEmptyCells.Count == 0)
        {
            return false;
        }

        if (nonEmptyCells.Count == 1)
        {
            return !int.TryParse(nonEmptyCells[0], out _);
        }

        return true;
    }

    private static ExtractedRow ExtractFields(IReadOnlyList<string> headers, IReadOnlyList<string> cells)
    {
        var nonEmptyCells = cells.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        var itemIndex = FindHeaderIndex(headers, "item", "items", "category name");
        var brandIndex = FindHeaderIndex(headers, "brand");
        var serialIndex = FindHeaderIndex(headers, "serial no", "serial number");
        var statusIndex = FindHeaderIndex(headers, "status active/not active", "status", "active/not active");
        var moveableIndex = FindHeaderIndex(headers, "moveable");
        var stationaryIndex = FindHeaderIndex(headers, "stationary");
        var numberIndexes = headers
            .Select((value, index) => new { value, index })
            .Where(x => x.value is "no" or "s/n")
            .Select(x => x.index)
            .ToList();

        var sequenceNumber = numberIndexes.Count > 0 ? GetCell(cells, numberIndexes[0]) : nonEmptyCells.FirstOrDefault();
        var itemName = GetCell(cells, itemIndex)
            ?? nonEmptyCells.Skip(int.TryParse(nonEmptyCells.FirstOrDefault(), out _) ? 1 : 0).FirstOrDefault();

        var moveableCount = ParseNullableInt(GetCell(cells, moveableIndex));
        var stationaryCount = ParseNullableInt(GetCell(cells, stationaryIndex));

        int? quantity = null;
        string? quantityText = null;

        if (moveableCount.HasValue || stationaryCount.HasValue)
        {
            quantity = (moveableCount ?? 0) + (stationaryCount ?? 0);
            quantityText = quantity.ToString();
        }
        else
        {
            var quantityIndex = numberIndexes.LastOrDefault();
            if (quantityIndex > 0)
            {
                quantityText = GetCell(cells, quantityIndex);
                quantity = ParseNullableInt(quantityText);
            }

            if (!quantity.HasValue)
            {
                var fallbackQuantity = nonEmptyCells
                    .SkipWhile(v => v == sequenceNumber)
                    .SkipWhile(v => v == itemName)
                    .FirstOrDefault(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
                quantityText = fallbackQuantity;
                quantity = ParseNullableInt(fallbackQuantity);
            }
        }

        var brand = GetCell(cells, brandIndex);
        var serialNumber = GetCell(cells, serialIndex);
        var status = GetCell(cells, statusIndex);

        if (string.IsNullOrWhiteSpace(status))
        {
            status = nonEmptyCells.LastOrDefault(v => v.Contains("active", StringComparison.OrdinalIgnoreCase) || v.Contains("bad", StringComparison.OrdinalIgnoreCase));
        }

        return new ExtractedRow(
            NormalizeCell(sequenceNumber),
            NormalizeCell(itemName),
            quantity,
            NormalizeCell(quantityText),
            moveableCount,
            stationaryCount,
            NormalizeCell(brand),
            NormalizeCell(serialNumber),
            NormalizeCell(status));
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] candidates)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (candidates.Contains(headers[i], StringComparer.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? GetCell(IReadOnlyList<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(cells[index]) ? null : cells[index].Trim();
    }

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim()
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string NormalizeHeaderCell(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NormalizeCell(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BsonValue ToBsonValue(string? value) => string.IsNullOrWhiteSpace(value) ? BsonNull.Value : value.Trim();

    private sealed record WorkbookSheet(string Name, List<WorkbookRow> Rows);
    private sealed record WorkbookRow(int RowNumber, List<string> Cells);
    private sealed record ExtractedRow(
        string? SequenceNumber,
        string? ItemName,
        int? Quantity,
        string? QuantityText,
        int? MoveableCount,
        int? StationaryCount,
        string? Brand,
        string? SerialNumber,
        string? Status);
}

public sealed record MongoWorkbookImportSummary(string BatchId, string DatabaseName, int SheetCount, int ImportedRows);
