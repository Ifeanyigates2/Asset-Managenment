using FrislEams.Web.Data;
using FrislEams.Web.Domain;
using FrislEams.Web.Models;
using FrislEams.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrislEams.Web.Controllers;

public class WorkflowController(
    AppDbContext db,
    AssetLifecycleService lifecycleService,
    RfidMonitoringService rfidMonitoringService,
    RoleGuard roleGuard,
    WorkflowNotificationService workflowNotifications) : Controller
{
    // ──────────────────── ASSET REQUESTS ────────────────────

    [HttpGet]
    public async Task<IActionResult> Requests()
    {
        await SetCurrentStaffViewBagAsync();
        ViewBag.Staff = await db.Staff.AsQueryable().Include(s => s.Department).ToListAsync();
        ViewBag.Assets = await db.Assets.ToListAsync();
        ViewBag.Departments = await db.Departments.ToListAsync();
        var requests = await db.AssetRequests
            .Include(r => r.RequestedByStaff)
            .Include(r => r.Department)
            .Include(r => r.Asset)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(AssetRequest vm)
    {
        var staff = await GetCurrentStaffAsync();
        if (staff is not null)
        {
            vm.RequestedByStaffId = staff.Id;
            vm.DepartmentId = staff.DepartmentId;
        }

        vm.CreatedAt = DateTime.UtcNow;
        vm.Status = "Pending Department Approval";
        db.AssetRequests.Add(vm);
        await db.SaveChangesAsync();

        var requesterName = staff?.FullName
            ?? (await db.Staff.FindAsync(vm.RequestedByStaffId))?.FullName
            ?? "A staff member";
        workflowNotifications.NotifyAssetRequestCreated(requesterName, vm.RequestType);
        await db.SaveChangesAsync();

        TempData["Success"] = "Asset request submitted.";
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDeptRequest(int id, string? approverName)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.DepartmentHead, RoleName.Admin)) return Forbid();

        var request = await db.AssetRequests.FindAsync(id);
        if (request is null) return NotFound();

        var resolvedApprover = string.IsNullOrWhiteSpace(approverName)
            ? HttpContext.Session.GetString("UserName") ?? "Department Head"
            : approverName.Trim();

        request.ApprovedByDepartmentHead = resolvedApprover;
        request.DeptHeadApprovedAt = DateTime.UtcNow;
        request.Status = "Pending Admin Approval";
        await db.SaveChangesAsync();
        TempData["Success"] = "Request approved at department level.";
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAdminRequest(int id, string approverName)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin)) return Forbid();
        var request = await db.AssetRequests.FindAsync(id);
        if (request is null) return NotFound();
        request.ApprovedByAdmin = approverName;
        request.AdminApprovedAt = DateTime.UtcNow;
        request.Status = "Approved – Pending Fulfilment";
        await db.SaveChangesAsync();
        TempData["Success"] = "Request approved by Admin.";
        return RedirectToAction(nameof(Requests));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, string reason)
    {
        var request = await db.AssetRequests.FindAsync(id);
        if (request is null) return NotFound();
        request.Status = "Rejected";
        request.RejectionReason = reason;
        await db.SaveChangesAsync();
        TempData["Success"] = "Request rejected.";
        return RedirectToAction(nameof(Requests));
    }

    // ──────────────────── REPAIRS ────────────────────

    [HttpGet]
    public async Task<IActionResult> Repairs()
    {
        await SetCurrentStaffViewBagAsync();
        ViewBag.Assets = await db.Assets
            .Where(a => a.CurrentStatus == AssetStatus.ActiveAssigned || a.CurrentStatus == AssetStatus.Damaged || a.CurrentStatus == AssetStatus.UnderRepair)
            .Include(a => a.CurrentDepartment)
            .ToListAsync();
        ViewBag.Staff = await db.Staff.ToListAsync();
        ViewBag.Contractors = await db.RepairContractors.AsQueryable().Where(c => c.IsActive).ToListAsync();
        var repairs = await db.RepairRequests
            .Include(r => r.Asset)
            .Include(r => r.ReportedByStaff)
            .Include(r => r.AssignedContractor)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(repairs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseRepair(RepairRequestVm vm)
    {
        var staff = await GetCurrentStaffAsync();
        if (staff is not null)
        {
            vm.ReportedByStaffId = staff.Id;
        }

        var asset = await db.Assets.FindAsync(vm.AssetId);
        var reporterName = staff?.FullName
            ?? (await db.Staff.FindAsync(vm.ReportedByStaffId))?.FullName
            ?? "A staff member";

        db.RepairRequests.Add(new RepairRequest
        {
            AssetId = vm.AssetId,
            ReportedByStaffId = vm.ReportedByStaffId,
            Description = vm.Description,
            Severity = vm.Severity,
            PreferredAction = vm.PreferredAction,
            Status = "Pending Admin Review",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        workflowNotifications.NotifyRepairRequestCreated(
            reporterName,
            asset?.TagCode ?? $"asset #{vm.AssetId}",
            vm.Severity);
        await db.SaveChangesAsync();

        TempData["Success"] = "Repair request raised.";
        return RedirectToAction(nameof(Repairs));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRepair(ApproveRepairVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin)) return Forbid();

        var repair = await db.RepairRequests.AsQueryable().Include(r => r.Asset).FirstOrDefaultAsync(r => r.Id == vm.RepairRequestId);
        if (repair?.Asset is null) return NotFound();

        repair.ApprovedAction = vm.Action;
        repair.AssignedContractorId = vm.ContractorId;
        repair.Status = $"Approved: {vm.Action}";

        if (vm.Action.Equals("Repair", StringComparison.OrdinalIgnoreCase))
            lifecycleService.ChangeStatus(repair.Asset, AssetStatus.UnderRepair, vm.Reason, "Admin");
        else if (vm.Action.Equals("Replacement", StringComparison.OrdinalIgnoreCase))
            lifecycleService.ChangeStatus(repair.Asset, AssetStatus.PendingReplacement, vm.Reason, "Admin");
        else if (vm.Action.Equals("Discard", StringComparison.OrdinalIgnoreCase))
            lifecycleService.ChangeStatus(repair.Asset, AssetStatus.Discarded, vm.Reason, "Admin");

        await db.SaveChangesAsync();
        TempData["Success"] = "Repair request approved.";
        return RedirectToAction(nameof(Repairs));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRepair(int repairId, string notes, decimal? cost)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin)) return Forbid();
        var repair = await db.RepairRequests.AsQueryable().Include(r => r.Asset).FirstOrDefaultAsync(r => r.Id == repairId);
        if (repair?.Asset is null) return NotFound();

        repair.Status = "Completed";
        repair.RepairNotes = notes;
        repair.RepairCost = cost;
        repair.CompletedAt = DateTime.UtcNow;

        lifecycleService.ChangeStatus(repair.Asset, AssetStatus.ActiveAssigned, "Repair completed – returned to service", "Admin");
        await db.SaveChangesAsync();
        TempData["Success"] = "Repair marked as complete. Asset returned to active.";
        return RedirectToAction(nameof(Repairs));
    }

    // ──────────────────── LOANS ────────────────────

    [HttpGet]
    public async Task<IActionResult> Loans()
    {
        await SetCurrentStaffViewBagAsync();
        ViewBag.Assets = await db.Assets
            .Where(a => a.CurrentStatus == AssetStatus.ActiveAssigned)
            .Include(a => a.CurrentDepartment)
            .ToListAsync();
        ViewBag.Staff = await db.Staff.ToListAsync();
        var loans = await db.LoanRequests
            .Include(l => l.Asset)
            .Include(l => l.RequestedByStaff)
            .OrderByDescending(l => l.Id)
            .ToListAsync();
        return View(loans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseLoan(LoanRequestVm vm)
    {
        var staff = await GetCurrentStaffAsync();
        if (staff is not null)
        {
            vm.RequestedByStaffId = staff.Id;
        }

        var asset = await db.Assets.FindAsync(vm.AssetId);
        var requesterName = staff?.FullName
            ?? (await db.Staff.FindAsync(vm.RequestedByStaffId))?.FullName
            ?? "A staff member";

        db.LoanRequests.Add(new LoanRequest
        {
            AssetId = vm.AssetId,
            RequestedByStaffId = vm.RequestedByStaffId,
            LoanType = vm.LoanType,
            StartDate = vm.StartDate,
            ExpectedReturnDate = vm.ExpectedReturnDate,
            Purpose = vm.Purpose,
            Destination = vm.Destination,
            ResponsiblePerson = vm.ResponsiblePerson,
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        workflowNotifications.NotifyLoanRequestCreated(
            requesterName,
            asset?.TagCode ?? $"asset #{vm.AssetId}",
            vm.LoanType);
        await db.SaveChangesAsync();

        TempData["Success"] = "Loan request submitted.";
        return RedirectToAction(nameof(Loans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLoan(ApproveLoanVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.DepartmentHead)) return Forbid();

        var loan = await db.LoanRequests.AsQueryable().Include(l => l.Asset).FirstOrDefaultAsync(l => l.Id == vm.LoanRequestId);
        if (loan?.Asset is null) return NotFound();

        loan.ApprovedBy = vm.ApprovedBy;
        loan.ApprovedAt = DateTime.UtcNow;
        loan.Status = "Approved";

        var isExternal = loan.LoanType.Equals("External", StringComparison.OrdinalIgnoreCase);
        lifecycleService.ChangeStatus(loan.Asset,
            isExternal ? AssetStatus.LoanedOutExternal : AssetStatus.LoanedOutInternal,
            "Loan approved", vm.ApprovedBy);

        if (isExternal)
        {
            db.ExitGrants.Add(new ExitGrant
            {
                AssetId = loan.AssetId,
                LoanRequestId = loan.Id,
                GrantedBy = vm.ApprovedBy,
                GrantStartDate = loan.StartDate,
                GrantEndDate = loan.ExpectedReturnDate,
                IsActive = true,
                ExitReason = loan.Purpose
            });
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Loan approved.";
        return RedirectToAction(nameof(Loans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLoan(RejectLoanVm vm)
    {
        if (!roleGuard.HasAnyRole(this, RoleName.Admin, RoleName.DepartmentHead)) return Forbid();

        var loan = await db.LoanRequests.AsQueryable().Include(l => l.Asset).FirstOrDefaultAsync(l => l.Id == vm.LoanRequestId);
        if (loan is null) return NotFound();

        if (!string.Equals(loan.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only pending loan requests can be rejected.";
            return RedirectToAction(nameof(Loans));
        }

        var reason = string.IsNullOrWhiteSpace(vm.RejectionReason)
            ? "Loan request declined"
            : vm.RejectionReason.Trim();

        loan.Status = "Rejected";
        loan.RejectedBy = string.IsNullOrWhiteSpace(vm.RejectedBy)
            ? HttpContext.Session.GetString("UserName") ?? "Approver"
            : vm.RejectedBy.Trim();
        loan.RejectedAt = DateTime.UtcNow;
        loan.RejectionReason = reason;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Loan request rejected: {reason}";
        return RedirectToAction(nameof(Loans));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnLoan(int loanId)
    {
        var loan = await db.LoanRequests.AsQueryable().Include(l => l.Asset).FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan?.Asset is null) return NotFound();

        loan.ActualReturnDate = DateTime.UtcNow;
        loan.Status = "Returned";

        // Close exit grant if any
        var grant = await db.ExitGrants.FirstOrDefaultAsync(g => g.LoanRequestId == loanId && g.IsActive);
        if (grant is not null)
        {
            grant.IsActive = false;
        }

        lifecycleService.ChangeStatus(loan.Asset, AssetStatus.ActiveAssigned, "Asset returned from loan", "Admin");
        await db.SaveChangesAsync();
        TempData["Success"] = "Asset returned from loan.";
        return RedirectToAction(nameof(Loans));
    }

    // ──────────────────── RFID ────────────────────

    [HttpGet]
    public async Task<IActionResult> Rfid()
    {
        var events = await db.RfidEvents
            .Include(e => e.Asset)
            .OrderByDescending(e => e.EventTime)
            .Take(200)
            .ToListAsync();
        ViewBag.AlertCount = await db.RfidEvents.CountAsync(e => e.AlertTriggered);
        ViewBag.TotalEvents = await db.RfidEvents.CountAsync();
        ViewBag.ActiveGrants = await db.ExitGrants.CountAsync(g => g.IsActive);
        return View(events);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RfidScan(RfidScanVm vm)
    {
        var (outcome, alert, message) = await rfidMonitoringService.ProcessDoorScanAsync(vm.RfidCode.Trim(), vm.DoorLocation.Trim());
        TempData[alert ? "Error" : "Success"] = message;
        return RedirectToAction(nameof(Rfid));
    }

    private async Task<Staff?> GetCurrentStaffAsync()
    {
        var username = HttpContext.Session.GetString("LoginUsername");
        var displayName = HttpContext.Session.GetString("UserName");
        return await StaffRepository.FindBySessionAsync(db, username, displayName);
    }

    private async Task SetCurrentStaffViewBagAsync()
    {
        var staff = await GetCurrentStaffAsync();
        ViewBag.CurrentStaff = staff;
        ViewBag.CurrentStaffId = staff?.Id;
        ViewBag.CurrentDepartmentId = staff?.DepartmentId;
    }
}
