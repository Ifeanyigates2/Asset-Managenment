namespace FrislEams.Web.Services;

/// <summary>
/// Portal access matrix aligned with FRISL role portal specification.
/// </summary>
public static class PortalCapabilities
{
    public static bool CanViewAssets(string portal) => true;

    public static bool CanCreateEditAssets(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal;

    public static bool CanAssignMoveAssets(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal or PortalService.StaffPortal;

    public static bool CanEncodeRfid(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal;

    public static bool CanReconcile(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal or PortalService.AuditorPortal;

    public static bool CanViewReports(string portal) => true;

    public static bool CanManageUsers(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal;

    public static bool CanManageSystemSettings(string portal)
        => portal == PortalService.AdminPortal;

    public static bool CanViewAuditLogs(string portal)
        => portal is PortalService.AdminPortal or PortalService.AuditorPortal;

    public static bool CanManageOperations(string portal)
        => portal is PortalService.BackofficePortal or PortalService.AdminPortal;

    public static bool IsAuditorReadOnly(string portal)
        => portal == PortalService.AuditorPortal;
}
