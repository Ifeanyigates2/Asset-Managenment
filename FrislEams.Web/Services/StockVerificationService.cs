using FrislEams.Web.Data;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class StockVerificationService(AppDbContext db, SystemAuditService audit)
{
    public async Task<StockVerificationSession> StartSessionAsync(int? departmentId, int? locationId, string username)
    {
        var session = new StockVerificationSession
        {
            DepartmentId = departmentId,
            LocationId = locationId,
            InitiatedBy = username,
            Status = "InProgress"
        };
        db.StockVerificationSessions.Add(session);
        await db.SaveChangesAsync();
        await audit.LogAsync(username, "Start Stock Verification", "StockVerificationSession", session.Id.ToString());
        return session;
    }

    public async Task<(bool Success, string Message, StockVerificationScan? Scan)> ProcessScanAsync(int sessionId, string rfidCode, string username)
    {
        var session = await db.StockVerificationSessions.FindAsync(sessionId);
        if (session is null || session.Status != "InProgress")
            return (false, "Session not found or already completed.", null);

        var tag = await db.RfidTags.AsQueryable().FirstOrDefaultAsync(t => t.RfidCode == rfidCode);
        var priorScans = await db.StockVerificationScans.AsQueryable()
            .Where(s => s.SessionId == sessionId && s.RfidCode == rfidCode)
            .CountAsync();

        string resultType;
        int? assetId = tag?.AssetId;

        if (priorScans > 0)
        {
            resultType = "Duplicate";
            session.DuplicateCount++;
        }
        else if (tag?.AssetId is null)
        {
            resultType = "Unexpected";
            session.UnexpectedCount++;
        }
        else
        {
            var asset = await db.Assets.FindAsync(tag.AssetId.Value);
            var inScope = await IsAssetInScopeAsync(asset, session);
            if (inScope)
            {
                resultType = "Found";
                session.FoundCount++;
            }
            else
            {
                resultType = "Unexpected";
                session.UnexpectedCount++;
            }
        }

        var scan = new StockVerificationScan
        {
            SessionId = sessionId,
            RfidCode = rfidCode,
            AssetId = assetId,
            ResultType = resultType
        };
        db.StockVerificationScans.Add(scan);
        await db.SaveChangesAsync();
        return (true, $"Scan recorded as {resultType}.", scan);
    }

    public async Task<StockVerificationSession> CompleteSessionAsync(int sessionId, string username)
    {
        var session = await db.StockVerificationSessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        var expected = await GetExpectedAssetIdsAsync(session);
        var foundIds = await db.StockVerificationScans.AsQueryable()
            .Where(s => s.SessionId == sessionId && s.ResultType == "Found" && s.AssetId.HasValue)
            .Select(s => s.AssetId!.Value)
            .Distinct()
            .ToListAsync();

        var missing = expected.Except(foundIds).Count();
        session.MissingCount = missing;
        session.Status = "Completed";
        session.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await audit.LogAsync(username, "Complete Stock Verification", "StockVerificationSession", sessionId.ToString());
        return session;
    }

    public async Task<List<StockVerificationScan>> GetScansAsync(int sessionId)
        => await db.StockVerificationScans.AsQueryable().Where(s => s.SessionId == sessionId).OrderByDescending(s => s.ScannedAt).ToListAsync();

    private async Task<HashSet<int>> GetExpectedAssetIdsAsync(StockVerificationSession session)
    {
        var assets = await db.Assets.AsQueryable().ToListAsync();
        return assets
            .Where(a => IsAssetInScope(a, session))
            .Select(a => a.Id)
            .ToHashSet();
    }

    private static bool IsAssetInScope(Asset? asset, StockVerificationSession session)
    {
        if (asset is null) return false;
        if (session.DepartmentId.HasValue && asset.CurrentDepartmentId != session.DepartmentId)
            return false;
        if (session.LocationId.HasValue && asset.CurrentLocationId != session.LocationId)
            return false;
        return true;
    }

    private static Task<bool> IsAssetInScopeAsync(Asset? asset, StockVerificationSession session)
        => Task.FromResult(IsAssetInScope(asset, session));
}
