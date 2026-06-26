using FrislEams.Web.Data;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class AuditService(AppDbContext db)
{
    public async Task<AuditSession> StartSessionAsync(StartAuditVm vm)
    {
        var session = new AuditSession
        {
            AuditType = vm.AuditType,
            DepartmentId = vm.DepartmentId,
            InitiatedBy = vm.InitiatedBy,
            Status = "Open",
            StartDate = DateTime.UtcNow
        };

        db.AuditSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task SubmitResultAsync(SubmitAuditResultVm vm)
    {
        var existing = await db.AuditResults.AsQueryable()
            .FirstOrDefaultAsync(a => a.AuditSessionId == vm.AuditSessionId && a.AssetId == vm.AssetId);
        if (existing is not null)
        {
            existing.SeenStatus = vm.SeenStatus;
            existing.PhysicalCondition = vm.PhysicalCondition;
            existing.Notes = vm.Notes;
            db.AuditResults.Update(existing);
            await db.SaveChangesAsync();
            return;
        }

        db.AuditResults.Add(new AuditResult
        {
            AuditSessionId = vm.AuditSessionId,
            AssetId = vm.AssetId,
            SeenStatus = vm.SeenStatus,
            PhysicalCondition = vm.PhysicalCondition,
            Notes = vm.Notes,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task CloseSessionAsync(int sessionId)
    {
        var session = await db.AuditSessions.FindAsync(sessionId);
        if (session is null)
        {
            return;
        }

        session.Status = "Closed";
        session.EndDate = DateTime.UtcNow;
        db.AuditSessions.Update(session);
        await db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, int>> BuildVarianceAsync(int sessionId)
    {
        var results = await db.AuditResults.AsQueryable().Where(r => r.AuditSessionId == sessionId).ToListAsync();

        var seen = results.Count(r => r.SeenStatus == "Seen");
        var missing = results.Count(r => r.SeenStatus == "Missing");
        var misplaced = results.Count(r => r.SeenStatus == "Misplaced");

        return new Dictionary<string, int>
        {
            ["Seen"] = seen,
            ["Missing"] = missing,
            ["Misplaced"] = misplaced,
            ["Total"] = results.Count
        };
    }
}
