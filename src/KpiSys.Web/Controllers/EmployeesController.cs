using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

public class EmployeesController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IOrganizationService _organizationService;

    public EmployeesController(IEmployeeService employeeService, IOrganizationService organizationService)
    {
        _employeeService = employeeService;
        _organizationService = organizationService;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] EmployeeFilter filter)
    {
        var employees = _employeeService.Search(filter);
        var organizations = _organizationService.GetAll().ToList();

        var model = new EmployeeListViewModel
        {
            Filter = filter,
            Employees = employees.Select(ToSummary).ToList(),
            TotalEmployees = _employeeService.GetAll().Count,
            DepartmentCount = organizations.Count,
            SearchResultCount = employees.Count,
            Organizations = organizations,
            Managers = _employeeService.GetAll().ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Edit", BuildFormModel(new EmployeeFormViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View("Edit", BuildFormModel(form));
        }

        var employee = new Employee
        {
            EmployeeNo = form.EmployeeNo,
            Name = form.Name,
            Email = form.Email,
            OrgId = form.OrgId,
            Title = form.Title,
            ManagerId = form.ManagerId
        };

        var (success, error) = _employeeService.Create(employee);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增失敗");
            return View("Edit", BuildFormModel(form));
        }

        TempData["Message"] = "員工已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var employee = _employeeService.GetById(id);
        if (employee == null)
        {
            return NotFound();
        }

        var model = new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeNo = employee.EmployeeNo,
            Name = employee.Name,
            Email = employee.Email,
            OrgId = employee.OrgId,
            Title = employee.Title,
            ManagerId = employee.ManagerId
        };

        return View(BuildFormModel(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildFormModel(form));
        }

        var updated = new Employee
        {
            EmployeeNo = form.EmployeeNo,
            Name = form.Name,
            Email = form.Email,
            OrgId = form.OrgId,
            Title = form.Title,
            ManagerId = form.ManagerId
        };

        var (success, error) = _employeeService.Update(id, updated);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "更新失敗");
            return View(BuildFormModel(form));
        }

        TempData["Message"] = "員工已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var (success, error) = _employeeService.Delete(id);
        TempData["Message"] = success ? "員工已刪除" : error ?? "刪除失敗";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var employee = _employeeService.GetById(id);
        if (employee == null)
        {
            return NotFound();
        }

        var orgName = _organizationService.GetById(employee.OrgId)?.OrgName ?? employee.OrgId;
        var managerName = employee.ManagerId.HasValue ? _employeeService.GetById(employee.ManagerId.Value)?.Name : null;

        var model = new EmployeeDetailViewModel
        {
            Employee = employee,
            OrganizationName = orgName,
            ManagerName = managerName,
            NewRole = new EmployeeRole { EmployeeId = id }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddRole(int id, EmployeeRole role)
    {
        var (success, error) = _employeeService.AddRole(id, role);
        TempData["Message"] = success ? "職能已新增" : error ?? "新增失敗";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveRole(int id, int roleId)
    {
        var (success, error) = _employeeService.RemoveRole(id, roleId);
        TempData["Message"] = success ? "職能已移除" : error ?? "移除失敗";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetPrimaryRole(int id, int roleId)
    {
        var (success, error) = _employeeService.SetPrimaryRole(id, roleId);
        TempData["Message"] = success ? "主要職能已設定" : error ?? "設定失敗";
        return RedirectToAction(nameof(Details), new { id });
    }

    private EmployeeSummaryViewModel ToSummary(Employee employee)
    {
        return new EmployeeSummaryViewModel
        {
            Id = employee.Id,
            EmployeeNo = employee.EmployeeNo,
            Name = employee.Name,
            Email = employee.Email,
            Organization = _organizationService.GetById(employee.OrgId)?.OrgName ?? employee.OrgId,
            Title = employee.Title,
            Manager = employee.ManagerId.HasValue ? _employeeService.GetById(employee.ManagerId.Value)?.Name : null
        };
    }

    private EmployeeFormViewModel BuildFormModel(EmployeeFormViewModel model)
    {
        model.Organizations = _organizationService.GetAll().ToList();
        model.Managers = _employeeService.GetAll().ToList();
        return model;
    }
}
