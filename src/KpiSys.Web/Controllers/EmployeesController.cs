using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web;
using KpiSys.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class EmployeesController : Controller
{
    private readonly KpiSysDbContext _db;
    private readonly IEmployeeService _employeeService;
    private readonly IOrganizationService _organizationService;

    public EmployeesController(
        KpiSysDbContext db,
        IEmployeeService employeeService,
        IOrganizationService organizationService)
    {
        _db = db;
        _employeeService = employeeService;
        _organizationService = organizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EmployeeFilter filter)
    {
        var query = _db.Employees
            .Include(e => e.Organization)
            .Include(e => e.Supervisor)
            .Include(e => e.Roles)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(e =>
                e.EmployeeNo.Contains(keyword) ||
                e.EmployeeName.Contains(keyword) ||
                (e.Email != null && e.Email.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(filter.EmployeeNo))
        {
            query = query.Where(e => e.EmployeeNo.Contains(filter.EmployeeNo.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(e => e.EmployeeName.Contains(filter.Name.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrgId))
        {
            var orgId = filter.OrgId.Trim();
            query = query.Where(e => e.OrgId.Equals(orgId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(e => e.Status.Equals(filter.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim();
            query = query.Where(e => e.Roles.Any(r => r.RoleCode.Contains(title)));
        }

        if (!string.IsNullOrWhiteSpace(filter.ManagerId))
        {
            var managerId = filter.ManagerId.Trim();
            query = query.Where(e => e.SupervisorId != null && e.SupervisorId.Equals(managerId, StringComparison.OrdinalIgnoreCase));
        }

        var employees = await query
            .OrderBy(e => e.EmployeeNo)
            .Select(e => new EmployeeSummaryViewModel
            {
                Id = e.EmployeeId,
                EmployeeNo = e.EmployeeNo,
                Name = e.EmployeeName,
                Email = e.Email,
                Organization = e.Organization != null ? e.Organization.OrgName : string.Empty,
                Title = e.Roles.OrderByDescending(r => r.IsPrimary).Select(r => r.RoleCode).FirstOrDefault() ?? string.Empty,
                Manager = e.Supervisor != null ? e.Supervisor.EmployeeName : null
            })
            .ToListAsync();

        var organizations = await _db.Organizations.AsNoTracking().OrderBy(o => o.OrgName).ToListAsync();
        var managers = await _db.Employees.AsNoTracking()
            .OrderBy(e => e.EmployeeNo)
            .Select(e => new EmployeeSummaryViewModel
            {
                Id = e.EmployeeId,
                EmployeeNo = e.EmployeeNo,
                Name = e.EmployeeName
            })
            .ToListAsync();

        var model = new EmployeeListViewModel
        {
            Filter = filter,
            Employees = employees,
            TotalEmployees = await _db.Employees.CountAsync(),
            DepartmentCount = await _db.Organizations.CountAsync(o => o.IsActive),
            SearchResultCount = employees.Count,
            Organizations = organizations.Select(o => new Organization { OrgId = o.OrgId.ToString(), OrgName = o.OrgName }).ToList(),
            Managers = managers
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
            Id = employee.Id.ToString(),
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
