using FrislEams.Web.Domain;

namespace FrislEams.Web.Models;

public class AssetCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int UsefulLifeYears { get; set; }
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LocationType { get; set; } = "Branch";
    public int? ParentLocationId { get; set; }
}

public class AssetType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
}

public class Manufacturer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Staff
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class RepairContractor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public string SlaHours { get; set; } = "48";
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class Asset
{
    public int Id { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string? GlCode { get; set; }
    public string StateOfPurchase { get; set; } = "New";
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Brand { get; set; }
    public string? TagNumber { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int? ExpectedServiceYears { get; set; }
    public string CurrentCondition { get; set; } = "Good";
    public AssetStatus CurrentStatus { get; set; } = AssetStatus.UnregisteredUnassigned;
    public int? CurrentLocationId { get; set; }
    public Location? CurrentLocation { get; set; }
    public int? CurrentDepartmentId { get; set; }
    public Department? CurrentDepartment { get; set; }
    public int? CurrentCustodianId { get; set; }
    public Staff? CurrentCustodian { get; set; }
    public string? InvoicePath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RfidTag
{
    public int Id { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string RfidCode { get; set; } = string.Empty;
    public string TagStatus { get; set; } = RfidTagStatus.Blank;
    public bool IsActive { get; set; } = true;
    public string? EncodedBy { get; set; }
    public DateTime? EncodedAt { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class AssetTransfer
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int FromDepartmentId { get; set; }
    public Department? FromDepartment { get; set; }
    public int ToDepartmentId { get; set; }
    public Department? ToDepartment { get; set; }
    public int? FromLocationId { get; set; }
    public Location? FromLocation { get; set; }
    public int? ToLocationId { get; set; }
    public Location? ToLocation { get; set; }
    public string Status { get; set; } = "Pending";
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DepartureRfidScan { get; set; }
    public DateTime? DepartureScannedAt { get; set; }
    public string? ArrivalRfidScan { get; set; }
    public DateTime? ArrivalScannedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StockVerificationSession
{
    public int Id { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public int FoundCount { get; set; }
    public int MissingCount { get; set; }
    public int UnexpectedCount { get; set; }
    public int DuplicateCount { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class StockVerificationScan
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public StockVerificationSession? Session { get; set; }
    public string RfidCode { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public class SystemAuditLog
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class AssetAssignment
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int? AssignedToStaffId { get; set; }
    public Staff? AssignedToStaff { get; set; }
    public int? AssignedToDepartmentId { get; set; }
    public Department? AssignedToDepartment { get; set; }
    public int AssignedLocationId { get; set; }
    public Location? AssignedLocation { get; set; }
    public string AssignedCondition { get; set; } = "Good";
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public string? ConfirmedCondition { get; set; }
    public int? ConfirmedByStaffId { get; set; }
    public Staff? ConfirmedByStaff { get; set; }
    public int? RejectedByStaffId { get; set; }
    public Staff? RejectedByStaff { get; set; }
    public string? RejectionReason { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string AssignedBy { get; set; } = "Admin";
}

public class AssetStatusHistory
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public AssetStatus PreviousStatus { get; set; }
    public AssetStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? LocationSnapshotId { get; set; }
    public int? DepartmentSnapshotId { get; set; }
    public int? CustodianSnapshotId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class AssetRequest
{
    public int Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int RequestedByStaffId { get; set; }
    public Staff? RequestedByStaff { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending Department Approval";
    public string? ApprovedByDepartmentHead { get; set; }
    public DateTime? DeptHeadApprovedAt { get; set; }
    public string? ApprovedByAdmin { get; set; }
    public DateTime? AdminApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public int? FulfilledByAssetId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RepairRequest
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int ReportedByStaffId { get; set; }
    public Staff? ReportedByStaff { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string PreferredAction { get; set; } = "Repair";
    public string Status { get; set; } = "Pending Admin Review";
    public string? ApprovedAction { get; set; }
    public int? AssignedContractorId { get; set; }
    public RepairContractor? AssignedContractor { get; set; }
    public string? RepairNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? RepairCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LoanRequest
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int RequestedByStaffId { get; set; }
    public Staff? RequestedByStaff { get; set; }
    public string LoanType { get; set; } = "Internal";
    public DateTime StartDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string ResponsiblePerson { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class ExitGrant
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public int? LoanRequestId { get; set; }
    public LoanRequest? LoanRequest { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
    public DateTime GrantStartDate { get; set; }
    public DateTime GrantEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ExitReason { get; set; }
}

public class RfidEvent
{
    public int Id { get; set; }
    public string RfidCode { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string DoorLocation { get; set; } = string.Empty;
    public string ProcessedStatus { get; set; } = string.Empty;
    public bool AlertTriggered { get; set; }
    public string? AlertMessage { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}

public class AuditSession
{
    public int Id { get; set; }
    public string AuditType { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? Notes { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public List<AuditResult> Results { get; set; } = [];
}

public class AuditResult
{
    public int Id { get; set; }
    public int AuditSessionId { get; set; }
    public AuditSession? AuditSession { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string SeenStatus { get; set; } = string.Empty;
    public string? PhysicalCondition { get; set; }
    public string? Notes { get; set; }
    public string? Variance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public string TargetRole { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProcurementRecord
{
    public int Id { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public AssetCategory? Category { get; set; }
    public decimal UnitCost { get; set; }
    public int Quantity { get; set; }
    public string GlCode { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = "Synced";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class IntegrationEventLog
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = "Queued";
    public string? ProcessingNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public class AuditScanPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public string Auditor { get; set; } = string.Empty;
    public int? DistinctScannedCount { get; set; }
    public int? DiscrepancyCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditTemporaryScan
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public AuditScanPeriod? Period { get; set; }
    public int ScanNumber { get; set; }
    public string Source { get; set; } = "external_reader";
    public int ItemCount { get; set; }
    public int DuplicateInBatchCount { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public class AuditTemporaryScanItem
{
    public int Id { get; set; }
    public int ScanId { get; set; }
    public AuditTemporaryScan? Scan { get; set; }
    public string RfidCode { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public class AuditDiscrepancy
{
    public int Id { get; set; }
    public int PeriodId { get; set; }
    public AuditScanPeriod? Period { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RfidCode { get; set; } = string.Empty;
    public string? AssetTag { get; set; }
    public int? AssetId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Backoffice";
    public string? EmployeeId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Designation { get; set; }
    public int? DepartmentId { get; set; }
    public int? BranchLocationId { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
