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

    public ProjectsController(IProjectService projectService, ICodeService codeService, IEmployeeService employeeService)
    {
        _projectService = projectService;
        _codeService = codeService;
        _employeeService = employeeService;
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

        var model = new ProjectFormViewModel
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
            Description = project.Description
        };

        return View(BuildFormModel(model));
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

    private ProjectFormViewModel BuildFormModel(ProjectFormViewModel form)
    {
        form.ProjectTypes = _codeService.GetCodes("PROJECT_TYPE");
        form.ProjectSizes = _codeService.GetCodes("PROJECT_SIZE");
        form.ProjectCriticalities = _codeService.GetCodes("PROJECT_CRITICALITY");
        form.Portfolios = _codeService.GetCodes("PORTFOLIO");
        form.Statuses = _codeService.GetCodes("PROJECT_STATUS");
        form.Employees = _employeeService.GetAll().ToList();
        return form;
    }
}
