using System;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize("Manager", "PM")]
public class TimesheetApprovalsController : Controller
{
    private readonly ITimesheetService _timesheetService;
    private readonly IEmployeeService _employeeService;
    private readonly IProjectService _projectService;
    private readonly IUserService _userService;

    public TimesheetApprovalsController(
        ITimesheetService timesheetService,
        IEmployeeService employeeService,
        IProjectService projectService,
        IUserService userService)
    {
        _timesheetService = timesheetService;
        _employeeService = employeeService;
        _projectService = projectService;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] TimesheetApprovalFilterViewModel filter)
    {
        if (!TryGetReviewer(out var user, out var employee, out var errorResult))
        {
            return errorResult;
        }

        var defaultStart = filter.StartDate ?? DateTime.Today.AddDays(-7);
        var defaultEnd = filter.EndDate ?? DateTime.Today;

        var reviewFilter = new TimesheetReviewFilter
        {
            ProjectCode = filter.ProjectCode,
            EmployeeId = filter.EmployeeId,
            StartDate = defaultStart,
            EndDate = defaultEnd,
            ReviewerEmployeeId = employee.Id,
            ReviewerRole = user.Role
        };

        var entries = _timesheetService.GetSubmittedForReview(reviewFilter)
            .Select(MapToListItem)
            .ToList();

        var model = new TimesheetApprovalListViewModel
        {
            Filter = new TimesheetApprovalFilterViewModel
            {
                ProjectCode = filter.ProjectCode,
                EmployeeId = filter.EmployeeId,
                StartDate = defaultStart,
                EndDate = defaultEnd
            },
            Entries = entries,
            Projects = _projectService.GetAll().ToList(),
            Employees = _employeeService.GetAll().ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(int id, string? remarks, string? returnUrl)
    {
        return HandleReview(id, remarks, returnUrl, (timesheetId, reviewer) =>
            _timesheetService.Approve(timesheetId, reviewer.EmployeeId, reviewer.Role, remarks));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(int id, string? remarks, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            TempData["Error"] = "駁回時須填寫備註";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
        }

        return HandleReview(id, remarks, returnUrl, (timesheetId, reviewer) =>
            _timesheetService.Reject(timesheetId, reviewer.EmployeeId, reviewer.Role, remarks));
    }

    private IActionResult HandleReview(int id, string? remarks, string? returnUrl, Func<int, ReviewerContext, (bool success, string? error)> action)
    {
        if (!TryGetReviewer(out var user, out var employee, out var errorResult))
        {
            return errorResult;
        }

        var result = action(id, new ReviewerContext { EmployeeId = employee.Id, Role = user.Role });
        if (result.success)
        {
            TempData["Message"] = "已完成審核";
        }
        else
        {
            TempData["Error"] = result.error ?? "審核失敗";
        }

        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    private TimesheetApprovalListItem MapToListItem(TimesheetEntry entry)
    {
        var project = _projectService.GetByCode(entry.ProjectCode);
        var employee = _employeeService.GetById(entry.EmployeeId);
        var org = employee?.OrgId ?? string.Empty;

        return new TimesheetApprovalListItem
        {
            Id = entry.Id,
            WorkDate = entry.WorkDate,
            ProjectCode = entry.ProjectCode,
            ProjectName = project?.Name ?? entry.ProjectCode,
            EmployeeName = employee?.Name ?? entry.EmployeeId.ToString(),
            EmployeeOrg = org,
            Hours = entry.Hours,
            OvertimeHours = entry.OvertimeHours,
            Status = entry.Status,
            SubmittedAt = entry.SubmittedAt,
            ApprovalRemarks = entry.ApprovalRemarks
        };
    }

    private bool TryGetReviewer(out UserAccount user, out Employee employee, out IActionResult errorResult)
    {
        user = null!;
        employee = null!;
        errorResult = Forbid();

        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (!userId.HasValue)
        {
            return false;
        }

        var account = _userService.GetById(userId.Value);
        if (account == null || account.EmployeeId == null)
        {
            return false;
        }

        var actor = _employeeService.GetById(account.EmployeeId.Value);
        if (actor == null)
        {
            return false;
        }

        user = account;
        employee = actor;
        errorResult = null!;
        return true;
    }

    private class ReviewerContext
    {
        public int EmployeeId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
