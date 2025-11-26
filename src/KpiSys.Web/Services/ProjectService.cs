using System.Collections.Concurrent;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface IProjectService
{
    IReadOnlyList<Project> GetAll();
    IReadOnlyList<Project> Search(ProjectFilter filter);
    Project? GetByCode(string code);
    (bool success, string? error) Create(Project project);
    (bool success, string? error) Update(string code, Project updated);
    IReadOnlyList<ProjectMember> GetMembers(string projectCode);
}

public class ProjectService : IProjectService
{
    private readonly ConcurrentDictionary<string, Project> _projects = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<ProjectMember>> _projectMembers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IEmployeeService _employeeService;
    private int _projectId = 1;
    private int _projectMemberId = 1;

    public ProjectService(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
        Seed();
    }

    public IReadOnlyList<Project> GetAll() => _projects.Values.OrderBy(p => p.Code).ToList();

    public IReadOnlyList<Project> Search(ProjectFilter filter)
    {
        var query = _projects.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(p => p.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                  || p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProjectType))
        {
            query = query.Where(p => string.Equals(p.ProjectType, filter.ProjectType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(p => string.Equals(p.Status, filter.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.PmId.HasValue)
        {
            query = query.Where(p => p.PmId == filter.PmId.Value);
        }

        return query.OrderBy(p => p.Code).ToList();
    }

    public Project? GetByCode(string code)
    {
        return _projects.TryGetValue(code, out var project) ? Clone(project) : null;
    }

    public (bool success, string? error) Create(Project project)
    {
        var validation = Validate(project, null);
        if (!validation.success)
        {
            return validation;
        }

        project.Id = Interlocked.Increment(ref _projectId);
        project.Code = project.Code.Trim();
        _projects.TryAdd(project.Code, Clone(project));
        EnsurePmMember(project.Code, project.PmId);
        return (true, null);
    }

    public (bool success, string? error) Update(string code, Project updated)
    {
        if (!_projects.TryGetValue(code, out var existing))
        {
            return (false, "找不到專案");
        }

        var validation = Validate(updated, code);
        if (!validation.success)
        {
            return validation;
        }

        updated.Id = existing.Id;
        updated.Code = updated.Code.Trim();

        if (!string.Equals(code, updated.Code, StringComparison.OrdinalIgnoreCase))
        {
            _projects.TryRemove(code, out _);
            if (_projectMembers.TryRemove(code, out var members))
            {
                foreach (var member in members)
                {
                    member.ProjectCode = updated.Code;
                }

                _projectMembers[updated.Code] = members;
            }
        }

        _projects[updated.Code] = Clone(updated);
        EnsurePmMember(updated.Code, updated.PmId);
        return (true, null);
    }

    public IReadOnlyList<ProjectMember> GetMembers(string projectCode)
    {
        if (_projectMembers.TryGetValue(projectCode, out var members))
        {
            return members.Select(Clone).ToList();
        }

        return Array.Empty<ProjectMember>();
    }

    private (bool success, string? error) Validate(Project project, string? updatingCode)
    {
        if (string.IsNullOrWhiteSpace(project.Code))
        {
            return (false, "專案代碼必填");
        }

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            return (false, "專案名稱必填");
        }

        if (string.IsNullOrWhiteSpace(project.ProjectType))
        {
            return (false, "專案類型必填");
        }

        if (string.IsNullOrWhiteSpace(project.ProjectSize))
        {
            return (false, "專案規模必填");
        }

        if (string.IsNullOrWhiteSpace(project.ProjectCriticality))
        {
            return (false, "專案關鍵度必填");
        }

        if (string.IsNullOrWhiteSpace(project.Status))
        {
            return (false, "狀態必填");
        }

        if (_projects.Values.Any(p => p.Code.Equals(project.Code.Trim(), StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(p.Code, updatingCode, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "專案代碼重複");
        }

        if (project.StartDate.HasValue && project.EndDate.HasValue && project.StartDate > project.EndDate)
        {
            return (false, "開始日期不得晚於結束日期");
        }

        if (project.PmId.HasValue && _employeeService.GetById(project.PmId.Value) == null)
        {
            return (false, "PM 不存在於員工清單");
        }

        return (true, null);
    }

    private void EnsurePmMember(string projectCode, int? pmId)
    {
        if (!pmId.HasValue)
        {
            return;
        }

        var members = _projectMembers.GetOrAdd(projectCode, _ => new List<ProjectMember>());
        var existing = members.FirstOrDefault(m => string.Equals(m.Role, "PM", StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.EmployeeId = pmId.Value;
            existing.IsActive = true;
            return;
        }

        members.Add(new ProjectMember
        {
            Id = Interlocked.Increment(ref _projectMemberId),
            ProjectCode = projectCode,
            EmployeeId = pmId.Value,
            Role = "PM",
            AllocationPct = 100,
            IsActive = true
        });
    }

    private static Project Clone(Project project)
    {
        return new Project
        {
            Id = project.Id,
            Code = project.Code,
            Name = project.Name,
            ProjectType = project.ProjectType,
            ProjectSize = project.ProjectSize,
            ProjectCriticality = project.ProjectCriticality,
            Portfolio = project.Portfolio,
            PmId = project.PmId,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            Description = project.Description
        };
    }

    private static ProjectMember Clone(ProjectMember member)
    {
        return new ProjectMember
        {
            Id = member.Id,
            ProjectCode = member.ProjectCode,
            EmployeeId = member.EmployeeId,
            Role = member.Role,
            AllocationPct = member.AllocationPct,
            IsActive = member.IsActive
        };
    }

    private void Seed()
    {
        var pm = _employeeService.GetAll().FirstOrDefault();
        var seedProjects = new List<Project>
        {
            new()
            {
                Code = "PRJ-001",
                Name = "ERP 升級專案",
                ProjectType = "開發",
                ProjectSize = "L",
                ProjectCriticality = "重大",
                Portfolio = "FIN",
                PmId = pm?.Id,
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today.AddDays(90),
                Status = "執行中",
                Description = "升級 ERP 核心模組"
            },
            new()
            {
                Code = "PRJ-002",
                Name = "人資流程優化",
                ProjectType = "維運",
                ProjectSize = "M",
                ProjectCriticality = "重要",
                Portfolio = "HR",
                PmId = pm?.Id,
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today.AddDays(60),
                Status = "規劃中",
                Description = "優化人資系統與流程"
            }
        };

        foreach (var project in seedProjects)
        {
            project.Id = Interlocked.Increment(ref _projectId);
            _projects.TryAdd(project.Code, Clone(project));
            EnsurePmMember(project.Code, project.PmId);
        }
    }
}
