using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class TimesheetsController : Controller
{
    private readonly ITimesheetService _timesheetService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly IUserService _userService;
    private readonly IEmployeeService _employeeService;

    public TimesheetsController(
        ITimesheetService timesheetService,
        IProjectService projectService,
        ITaskService taskService,
        IUserService userService,
        IEmployeeService employeeService)
    {
        _timesheetService = timesheetService;
        _projectService = projectService;
        _taskService = taskService;
        _userService = userService;
        _employeeService = employeeService;
    }

    [HttpGet]
    public IActionResult Index(DateTime? date)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var referenceDate = (date ?? DateTime.Today).Date;
        var weekStart = StartOfWeek(referenceDate);
        var weekEnd = weekStart.AddDays(6);

        var entries = _timesheetService
            .GetByEmployeeAndRange(employeeId, weekStart, weekEnd)
            .Select(MapToListItem)
            .ToList();

        var model = new TimesheetListViewModel
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Entries = entries
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create(DateTime? workDate)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var entry = new TimesheetEntry
        {
            EmployeeId = employeeId,
            WorkDate = (workDate ?? DateTime.Today).Date,
            Status = "Draft"
        };

        return View("Edit", BuildFormModel(entry, employeeId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TimesheetFormViewModel form)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var model = BuildFormModel(form, employeeId);
        if (!ModelState.IsValid)
        {
            return View("Edit", model);
        }

        var entry = new TimesheetEntry
        {
            EmployeeId = employeeId,
            WorkDate = form.WorkDate.Date,
            ProjectCode = form.ProjectCode,
            TaskId = form.TaskId,
            Hours = form.Hours,
            OvertimeHours = form.OvertimeHours,
            Status = "Draft"
        };

        var (success, error) = _timesheetService.Create(entry);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增工時失敗");
            return View("Edit", BuildFormModel(form, employeeId));
        }

        TempData["Message"] = "工時已新增";
        return RedirectToAction(nameof(Index), new { date = form.WorkDate });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var entry = _timesheetService.GetById(id);
        if (entry == null)
        {
            return NotFound();
        }

        if (entry.EmployeeId != employeeId)
        {
            return Forbid();
        }

        return View(BuildFormModel(entry, employeeId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TimesheetFormViewModel form)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var model = BuildFormModel(form, employeeId);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = new TimesheetEntry
        {
            Id = id,
            EmployeeId = employeeId,
            WorkDate = form.WorkDate.Date,
            ProjectCode = form.ProjectCode,
            TaskId = form.TaskId,
            Hours = form.Hours,
            OvertimeHours = form.OvertimeHours,
            Status = form.Status
        };

        var (success, error) = _timesheetService.Update(id, updated, employeeId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "更新工時失敗");
            return View(BuildFormModel(form, employeeId));
        }

        TempData["Message"] = "工時已更新";
        return RedirectToAction(nameof(Index), new { date = form.WorkDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(int id)
    {
        if (!TryGetEmployeeId(out var employeeId, out var errorResult))
        {
            return errorResult;
        }

        var (success, error) = _timesheetService.Submit(id, employeeId);
        if (success)
        {
            TempData["Message"] = "工時已提交";
        }
        else
        {
            TempData["Error"] = error ?? "提交失敗";
        }

        return RedirectToAction(nameof(Index));
    }

    private TimesheetListItem MapToListItem(TimesheetEntry entry)
    {
        var project = _projectService.GetByCode(entry.ProjectCode);
        var task = entry.TaskId.HasValue
            ? _taskService.GetByProject(entry.ProjectCode).FirstOrDefault(t => t.Id == entry.TaskId.Value)
            : null;

        return new TimesheetListItem
        {
            Id = entry.Id,
            WorkDate = entry.WorkDate,
            ProjectCode = entry.ProjectCode,
            ProjectName = project?.Name ?? entry.ProjectCode,
            TaskName = task?.TaskName,
            Hours = entry.Hours,
            OvertimeHours = entry.OvertimeHours,
            Status = entry.Status,
            CanSubmit = string.Equals(entry.Status, "Draft", StringComparison.OrdinalIgnoreCase)
        };
    }

    private TimesheetFormViewModel BuildFormModel(TimesheetEntry entry, int employeeId)
    {
        var projects = GetEmployeeProjects(employeeId);
        var selectedProject = !string.IsNullOrWhiteSpace(entry.ProjectCode)
            ? projects.FirstOrDefault(p => string.Equals(p.Code, entry.ProjectCode, StringComparison.OrdinalIgnoreCase))
            : projects.FirstOrDefault();

        var projectCode = selectedProject?.Code ?? entry.ProjectCode;
        var tasks = string.IsNullOrWhiteSpace(projectCode) ? new List<ProjectTask>() : _taskService.GetByProject(projectCode).ToList();

        var model = new TimesheetFormViewModel
        {
            Id = entry.Id,
            WorkDate = entry.WorkDate == default ? DateTime.Today : entry.WorkDate.Date,
            ProjectCode = projectCode,
            TaskId = entry.TaskId,
            Hours = entry.Hours,
            OvertimeHours = entry.OvertimeHours,
            Status = entry.Status,
            ProjectOptions = projects.Select(p => new TimesheetProjectOption
            {
                Code = p.Code,
                Name = $"{p.Code} - {p.Name}"
            }).ToList(),
            TaskOptions = tasks.Select(t => new TimesheetTaskOption { Id = t.Id, Name = t.TaskName }).ToList(),
            ProjectTasks = projects.ToDictionary(
                p => p.Code,
                p => _taskService.GetByProject(p.Code).Select(t => new TimesheetTaskOption { Id = t.Id, Name = t.TaskName }).ToList(),
                StringComparer.OrdinalIgnoreCase)
        };

        return model;
    }

    private TimesheetFormViewModel BuildFormModel(TimesheetFormViewModel form, int employeeId)
    {
        var entry = new TimesheetEntry
        {
            Id = form.Id ?? 0,
            WorkDate = form.WorkDate,
            ProjectCode = form.ProjectCode,
            TaskId = form.TaskId,
            Hours = form.Hours,
            OvertimeHours = form.OvertimeHours,
            Status = form.Status
        };

        return BuildFormModel(entry, employeeId);
    }

    private bool TryGetEmployeeId(out int employeeId, out IActionResult errorResult)
    {
        employeeId = 0;
        errorResult = Forbid();

        var user = GetCurrentUser();
        if (user?.EmployeeId == null)
        {
            return false;
        }

        var employee = _employeeService.GetById(user.EmployeeId.Value);
        if (employee == null)
        {
            return false;
        }

        employeeId = employee.Id;
        errorResult = null!;
        return true;
    }

    private UserAccount? GetCurrentUser()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId.HasValue)
        {
            return _userService.GetById(userId.Value);
        }

        return _userService.GetAll().FirstOrDefault(u => string.Equals(u.Role, "Employee", StringComparison.OrdinalIgnoreCase));
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return date.AddDays(-diff).Date;
    }

    private List<Project> GetEmployeeProjects(int employeeId)
    {
        return _projectService.GetAll()
            .Where(p => _projectService.GetMembers(p.Code).Any(m => m.EmployeeId == employeeId))
            .OrderBy(p => p.Code)
            .ToList();
    }
}
