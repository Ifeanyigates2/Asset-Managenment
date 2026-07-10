using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;

namespace FrislEams.Web.Services;

public class WorkflowNotificationService(AppDbContext db)
{
    public void NotifyRoles(
        string title,
        string message,
        string type = "Info",
        string? linkUrl = null,
        params string[] roles)
    {
        var createdAt = DateTime.UtcNow;
        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            db.Notifications.Add(new Notification
            {
                TargetRole = role,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = createdAt
            });
        }
    }

    public void NotifyAssetRequestCreated(string requesterName, string requestType)
    {
        NotifyRoles(
            "New Asset Request",
            $"{requesterName} submitted a {requestType} asset request awaiting department approval.",
            "Info",
            "/Workflow/Requests",
            RoleName.DepartmentHead,
            RoleName.Backoffice,
            RoleName.Admin);
    }

    public void NotifyLoanRequestCreated(string requesterName, string assetTag, string loanType)
    {
        NotifyRoles(
            "New Loan Request",
            $"{requesterName} requested a {loanType} loan for asset {assetTag}.",
            "Info",
            "/Workflow/Loans",
            RoleName.DepartmentHead,
            RoleName.Backoffice,
            RoleName.Admin);
    }

    public void NotifyRepairRequestCreated(string reporterName, string assetTag, string severity)
    {
        NotifyRoles(
            "New Repair Request",
            $"{reporterName} reported a {severity} fault on {assetTag}. Awaiting admin review.",
            severity is "High" or "Critical" ? "Warning" : "Info",
            "/Workflow/Repairs",
            RoleName.DepartmentHead,
            RoleName.Backoffice,
            RoleName.Admin);
    }

    public void NotifyAssignmentPending(string assigneeName, string assetTag, string assetName)
    {
        NotifyRoles(
            "Assignment Awaiting Receipt",
            $"{assetName} ({assetTag}) was assigned to {assigneeName} and is pending receipt confirmation.",
            "Info",
            "/Assignments",
            RoleName.Staff,
            RoleName.DepartmentHead,
            RoleName.Backoffice,
            RoleName.Admin);
    }
}
