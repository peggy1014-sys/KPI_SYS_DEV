using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ICodeService _codeService;
    private readonly IEmployeeService _employeeService;
    private readonly ITaskService _taskService;

    public ProjectsController(IProjectService projectService, ICodeService codeService, IEmployeeService employeeService, ITaskService taskService)
    {
        _projectService = projectService;
        _codeService = codeService;
        _employeeService = employeeService;
        _taskService = taskService;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] ProjectFilter filter)
    {
        var projects = _projectService.Search(filter);
        var model = new ProjectListViewModel
        {
            Filter = filter,
            Projects = projects.Select(ToSummary).ToList(),
            ProjectTypes = _codeService.GetCodes("PROJECT_TYPE"),
            Statuses = _codeService.GetCodes("PROJECT_STATUS"),
            Employees = _employeeService.GetAll().ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new ProjectFormViewModel
        {
            Status = _codeService.GetCodes("PROJECT_STATUS").FirstOrDefault()?.Code ?? string.Empty
        };

        return View("Edit", BuildFormModel(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ProjectFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", BuildFormModel(form));
        }

        var project = MapToProject(form);
        var (success, error) = _projectService.Create(project);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增失敗");
            return View("Edit", BuildFormModel(form));
        }

        TempData["Message"] = "專案已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(string id)
    {
        var project = _projectService.GetByCode(id);
        if (project == null)
        {
            return NotFound();
        }

        return View(BuildFormModel(ToFormModel(project)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(string id, ProjectFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildFormModel(form));
        }

        var updated = MapToProject(form);
        var (success, error) = _projectService.Update(id, updated);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "更新失敗");
            return View(BuildFormModel(form));
        }

        TempData["Message"] = "專案已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMember(string id, ProjectMemberInput member)
    {
        var project = _projectService.GetByCode(id);
        if (project == null)
        {
            return NotFound();
        }

        member.ProjectCode = id;

        if (!ModelState.IsValid)
        {
            var model = BuildFormModel(ToFormModel(project));
            model.NewMember = member;
            return View("Edit", model);
        }

        var (success, error) = _projectService.AddMember(id, MapToMember(member));
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增成員失敗");
            var model = BuildFormModel(ToFormModel(project));
            model.NewMember = member;
            return View("Edit", model);
        }

        TempData["Message"] = "已新增專案成員";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddTask(string id, ProjectTaskInput task)
    {
        var project = _projectService.GetByCode(id);
        if (project == null)
        {
            return NotFound();
        }

        task.ProjectCode = id;

        if (!ModelState.IsValid)
        {
            var model = BuildFormModel(ToFormModel(project));
            model.NewTask = task;
            return View("Edit", model);
        }

        var newTask = new ProjectTask
        {
            ProjectCode = id,
            TaskName = task.TaskName,
            TaskGroup = task.TaskGroup,
            ResponsibleId = task.ResponsibleId,
            PlanStart = task.PlanStart,
            PlanEnd = task.PlanEnd,
            TaskProgress = 0
        };

        var (success, error) = _taskService.AddTask(newTask);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增任務失敗");
            var model = BuildFormModel(ToFormModel(project));
            model.NewTask = task;
            return View("Edit", model);
        }

        TempData["Message"] = "已新增專案任務";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveMember(string id, int memberId)
    {
        var project = _projectService.GetByCode(id);
        if (project == null)
        {
            return NotFound();
        }

        var (success, error) = _projectService.RemoveMember(id, memberId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "移除成員失敗");
            return View("Edit", BuildFormModel(ToFormModel(project)));
        }

        TempData["Message"] = "已移除專案成員";
        return RedirectToAction(nameof(Edit), new { id });
    }

    private ProjectSummaryViewModel ToSummary(Project project)
    {
        return new ProjectSummaryViewModel
        {
            Code = project.Code,
            Name = project.Name,
            ProjectType = project.ProjectType,
            PmName = project.PmId.HasValue ? _employeeService.GetById(project.PmId.Value)?.Name : null,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status
        };
    }

    private Project MapToProject(ProjectFormViewModel form)
    {
        return new Project
        {
            Code = form.Code,
            Name = form.Name,
            ProjectType = form.ProjectType,
            ProjectSize = form.ProjectSize,
            ProjectCriticality = form.ProjectCriticality,
            Portfolio = form.Portfolio,
            PmId = form.PmId,
            StartDate = form.StartDate,
            EndDate = form.EndDate,
            Status = form.Status,
            Description = form.Description
        };
    }

    private ProjectMember MapToMember(ProjectMemberInput input)
    {
        return new ProjectMember
        {
            ProjectCode = input.ProjectCode,
            EmployeeId = input.EmployeeId,
            Role = input.Role,
            AllocationPct = input.AllocationPct,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            IsActive = true
        };
    }

    private ProjectFormViewModel ToFormModel(Project project)
    {
        return new ProjectFormViewModel
        {
            OriginalCode = project.Code,
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
            Description = project.Description,
            NewMember = new ProjectMemberInput { ProjectCode = project.Code },
            NewTask = new ProjectTaskInput { ProjectCode = project.Code }
        };
    }

    private ProjectFormViewModel BuildFormModel(ProjectFormViewModel form)
    {
        form.ProjectTypes = _codeService.GetCodes("PROJECT_TYPE");
        form.ProjectSizes = _codeService.GetCodes("PROJECT_SIZE");
        form.ProjectCriticalities = _codeService.GetCodes("PROJECT_CRITICALITY");
        form.Portfolios = _codeService.GetCodes("PORTFOLIO");
        form.Statuses = _codeService.GetCodes("PROJECT_STATUS");
        form.TaskGroups = _codeService.GetCodes("TASK_GROUP");
        form.Employees = _employeeService.GetAll().ToList();
        form.NewMember ??= new ProjectMemberInput();
        if (string.IsNullOrEmpty(form.NewMember.ProjectCode))
        {
            form.NewMember.ProjectCode = form.Code;
        }

        form.NewTask ??= new ProjectTaskInput();
        if (string.IsNullOrEmpty(form.NewTask.ProjectCode))
        {
            form.NewTask.ProjectCode = form.Code;
        }

        if (!string.IsNullOrWhiteSpace(form.Code))
        {
            var members = _projectService.GetMembers(form.Code);
            form.Members = members.Select(m =>
            {
                var employee = form.Employees.FirstOrDefault(e => e.Id == m.EmployeeId);
                return new ProjectMemberViewModel
                {
                    Id = m.Id,
                    ProjectCode = m.ProjectCode,
                    EmployeeId = m.EmployeeId,
                    EmployeeNo = employee?.EmployeeNo ?? "",
                    EmployeeName = employee?.Name ?? "",
                    Role = m.Role,
                    AllocationPct = m.AllocationPct,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate
                };
            }).ToList();

            var tasks = _taskService.GetByProject(form.Code);
            form.Tasks = tasks.Select(t =>
            {
                var responsible = form.Employees.FirstOrDefault(e => e.Id == t.ResponsibleId);
                var groupName = form.TaskGroups.FirstOrDefault(g => string.Equals(g.Code, t.TaskGroup, StringComparison.OrdinalIgnoreCase))?.CodeName;
                return new ProjectTaskViewModel
                {
                    Id = t.Id,
                    ProjectCode = t.ProjectCode,
                    TaskName = t.TaskName,
                    TaskGroup = t.TaskGroup,
                    TaskGroupName = groupName,
                    ResponsibleId = t.ResponsibleId,
                    ResponsibleName = responsible?.Name,
                    PlanStart = t.PlanStart,
                    PlanEnd = t.PlanEnd,
                    TaskProgress = t.TaskProgress
                };
            }).ToList();
        }

        return form;
    }
}
