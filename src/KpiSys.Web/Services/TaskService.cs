using System.Collections.Concurrent;
using System.Threading;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface ITaskService
{
    IReadOnlyList<ProjectTask> GetByProject(string projectCode);
    (bool success, string? error) AddTask(ProjectTask task);
}

public class TaskService : ITaskService
{
    private readonly ConcurrentDictionary<string, List<ProjectTask>> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly IProjectService _projectService;
    private readonly ICodeService _codeService;
    private int _taskId;

    public TaskService(IProjectService projectService, ICodeService codeService)
    {
        _projectService = projectService;
        _codeService = codeService;
    }

    public IReadOnlyList<ProjectTask> GetByProject(string projectCode)
    {
        if (_tasks.TryGetValue(projectCode, out var tasks))
        {
            return tasks.Select(Clone).ToList();
        }

        return Array.Empty<ProjectTask>();
    }

    public (bool success, string? error) AddTask(ProjectTask task)
    {
        task.ProjectCode = task.ProjectCode?.Trim() ?? string.Empty;
        var validation = Validate(task);
        if (!validation.success)
        {
            return validation;
        }

        var list = _tasks.GetOrAdd(task.ProjectCode, _ => new List<ProjectTask>());
        task.Id = Interlocked.Increment(ref _taskId);
        task.TaskProgress = 0;
        list.Add(Clone(task));
        return (true, null);
    }

    private (bool success, string? error) Validate(ProjectTask task)
    {
        if (string.IsNullOrWhiteSpace(task.ProjectCode))
        {
            return (false, "專案代碼必填");
        }

        if (_projectService.GetByCode(task.ProjectCode) == null)
        {
            return (false, "找不到專案");
        }

        if (string.IsNullOrWhiteSpace(task.TaskName))
        {
            return (false, "任務名稱必填");
        }

        if (task.PlanStart.HasValue && task.PlanEnd.HasValue && task.PlanStart > task.PlanEnd)
        {
            return (false, "計畫開始日期不得晚於結束日期");
        }

        if (!string.IsNullOrWhiteSpace(task.TaskGroup))
        {
            var taskGroups = _codeService.GetCodes("TASK_GROUP");
            if (!taskGroups.Any(g => string.Equals(g.Code, task.TaskGroup, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "任務群組不存在");
            }

            task.TaskGroup = taskGroups.First(g => string.Equals(g.Code, task.TaskGroup, StringComparison.OrdinalIgnoreCase)).Code;
        }

        if (task.ResponsibleId.HasValue)
        {
            var members = _projectService.GetMembers(task.ProjectCode);
            if (!members.Any(m => m.EmployeeId == task.ResponsibleId.Value))
            {
                return (false, "負責人須為專案成員");
            }
        }

        return (true, null);
    }

    private static ProjectTask Clone(ProjectTask task)
    {
        return new ProjectTask
        {
            Id = task.Id,
            ProjectCode = task.ProjectCode,
            TaskName = task.TaskName,
            TaskGroup = task.TaskGroup,
            ResponsibleId = task.ResponsibleId,
            PlanStart = task.PlanStart,
            PlanEnd = task.PlanEnd,
            TaskProgress = task.TaskProgress
        };
    }
}
