using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface ITimesheetService
{
    IReadOnlyList<TimesheetEntry> GetByEmployeeAndRange(int employeeId, DateTime start, DateTime end);
    TimesheetEntry? GetById(int id);
    (bool success, string? error) Create(TimesheetEntry entry);
    (bool success, string? error) Update(int id, TimesheetEntry updated, int actorEmployeeId);
    (bool success, string? error) Submit(int id, int actorEmployeeId);
    IReadOnlyList<TimesheetAudit> GetAudits(int timesheetId);
}

public class TimesheetService : ITimesheetService
{
    private const string StatusDraft = "Draft";
    private const string StatusSubmitted = "Submitted";

    private readonly ConcurrentDictionary<int, TimesheetEntry> _timesheets = new();
    private readonly ConcurrentDictionary<int, List<TimesheetAudit>> _audits = new();
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private int _timesheetId;
    private int _auditId;

    public TimesheetService(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    public IReadOnlyList<TimesheetEntry> GetByEmployeeAndRange(int employeeId, DateTime start, DateTime end)
    {
        return _timesheets.Values
            .Where(t => t.EmployeeId == employeeId && t.WorkDate.Date >= start.Date && t.WorkDate.Date <= end.Date)
            .OrderBy(t => t.WorkDate)
            .ThenBy(t => t.ProjectCode)
            .ThenBy(t => t.TaskId ?? int.MaxValue)
            .Select(Clone)
            .ToList();
    }

    public TimesheetEntry? GetById(int id) => _timesheets.TryGetValue(id, out var entry) ? Clone(entry) : null;

    public (bool success, string? error) Create(TimesheetEntry entry)
    {
        entry.Status = StatusDraft;
        var validation = Validate(entry, null);
        if (!validation.success)
        {
            return validation;
        }

        entry.Id = Interlocked.Increment(ref _timesheetId);
        _timesheets[entry.Id] = Clone(entry);
        return (true, null);
    }

    public (bool success, string? error) Update(int id, TimesheetEntry updated, int actorEmployeeId)
    {
        if (!_timesheets.TryGetValue(id, out var existing))
        {
            return (false, "找不到工時");
        }

        if (!string.Equals(existing.Status, StatusDraft, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "僅能編輯草稿狀態");
        }

        if (existing.EmployeeId != actorEmployeeId)
        {
            return (false, "只能編輯自己的工時");
        }

        updated.Id = id;
        updated.EmployeeId = existing.EmployeeId;
        updated.Status = existing.Status;
        updated.SubmittedAt = existing.SubmittedAt;

        var validation = Validate(updated, id);
        if (!validation.success)
        {
            return validation;
        }

        _timesheets[id] = Clone(updated);
        return (true, null);
    }

    public (bool success, string? error) Submit(int id, int actorEmployeeId)
    {
        if (!_timesheets.TryGetValue(id, out var existing))
        {
            return (false, "找不到工時");
        }

        if (existing.EmployeeId != actorEmployeeId)
        {
            return (false, "只能提交自己的工時");
        }

        if (!string.Equals(existing.Status, StatusDraft, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "僅能提交草稿工時");
        }

        existing.Status = StatusSubmitted;
        existing.SubmittedAt = DateTime.Now;
        _timesheets[id] = Clone(existing);
        AddAudit(id, actorEmployeeId, "Submit");
        return (true, null);
    }

    public IReadOnlyList<TimesheetAudit> GetAudits(int timesheetId)
    {
        return _audits.TryGetValue(timesheetId, out var logs)
            ? logs.OrderByDescending(a => a.PerformedAt).Select(Clone).ToList()
            : Array.Empty<TimesheetAudit>();
    }

    private (bool success, string? error) Validate(TimesheetEntry entry, int? updatingId)
    {
        if (entry.WorkDate.Date > DateTime.Today)
        {
            return (false, "工作日期不可為未來日");
        }

        if (entry.Hours < 0 || entry.Hours > 24)
        {
            return (false, "工時需介於 0 到 24 小時");
        }

        if (entry.OvertimeHours < 0 || entry.OvertimeHours > 24)
        {
            return (false, "加班需介於 0 到 24 小時");
        }

        if (entry.Hours == 0 && entry.OvertimeHours == 0)
        {
            return (false, "工時與加班不可同時為 0");
        }

        if (string.IsNullOrWhiteSpace(entry.ProjectCode))
        {
            return (false, "專案必填");
        }

        entry.ProjectCode = entry.ProjectCode.Trim();
        var project = _projectService.GetByCode(entry.ProjectCode);
        if (project == null)
        {
            return (false, "找不到專案");
        }

        var members = _projectService.GetMembers(entry.ProjectCode);
        if (!members.Any(m => m.EmployeeId == entry.EmployeeId))
        {
            return (false, "僅能填報自己參與的專案");
        }

        if (entry.TaskId.HasValue)
        {
            var tasks = _taskService.GetByProject(entry.ProjectCode);
            if (!tasks.Any(t => t.Id == entry.TaskId.Value))
            {
                return (false, "任務不存在於該專案");
            }
        }

        var duplicate = _timesheets.Values.Any(t => t.EmployeeId == entry.EmployeeId
            && t.WorkDate.Date == entry.WorkDate.Date
            && string.Equals(t.ProjectCode, entry.ProjectCode, StringComparison.OrdinalIgnoreCase)
            && t.TaskId == entry.TaskId
            && t.Id != updatingId);

        if (duplicate)
        {
            return (false, "同日同專案與任務的工時已存在");
        }

        var dailyTotal = _timesheets.Values
            .Where(t => t.EmployeeId == entry.EmployeeId && t.WorkDate.Date == entry.WorkDate.Date && t.Id != updatingId)
            .Sum(t => t.Hours + t.OvertimeHours);

        if (dailyTotal + entry.Hours + entry.OvertimeHours > 24)
        {
            return (false, "當日總工時不可超過 24 小時");
        }

        return (true, null);
    }

    private void AddAudit(int timesheetId, int actorEmployeeId, string action)
    {
        var audit = new TimesheetAudit
        {
            Id = Interlocked.Increment(ref _auditId),
            TimesheetId = timesheetId,
            Action = action,
            PerformedBy = actorEmployeeId,
            PerformedAt = DateTime.Now,
        };

        var list = _audits.GetOrAdd(timesheetId, _ => new List<TimesheetAudit>());
        list.Add(audit);
    }

    private static TimesheetEntry Clone(TimesheetEntry entry)
    {
        return new TimesheetEntry
        {
            Id = entry.Id,
            EmployeeId = entry.EmployeeId,
            ProjectCode = entry.ProjectCode,
            TaskId = entry.TaskId,
            WorkDate = entry.WorkDate,
            Hours = entry.Hours,
            OvertimeHours = entry.OvertimeHours,
            Status = entry.Status,
            SubmittedAt = entry.SubmittedAt
        };
    }

    private static TimesheetAudit Clone(TimesheetAudit audit)
    {
        return new TimesheetAudit
        {
            Id = audit.Id,
            TimesheetId = audit.TimesheetId,
            Action = audit.Action,
            PerformedBy = audit.PerformedBy,
            PerformedAt = audit.PerformedAt,
            Notes = audit.Notes
        };
    }
}
