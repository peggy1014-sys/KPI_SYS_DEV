using System;
using System.Collections.Concurrent;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface IEmployeeService
{
    IReadOnlyList<Employee> GetAll();
    IReadOnlyList<Employee> Search(EmployeeFilter filter);
    Employee? GetById(int id);
    (bool success, string? error) Create(Employee employee);
    (bool success, string? error) Update(int id, Employee updated);
    (bool success, string? error) Delete(int id);

    IReadOnlyList<EmployeeRole> GetRoles(int employeeId);
    (bool success, string? error) AddRole(int employeeId, EmployeeRole role);
    (bool success, string? error) RemoveRole(int employeeId, int roleId);
    (bool success, string? error) SetPrimaryRole(int employeeId, int roleId);
}

public class EmployeeService : IEmployeeService
{
    private readonly ConcurrentDictionary<int, Employee> _employees = new();
    private readonly IOrganizationService _organizationService;
    private int _employeeId = 1000;
    private int _employeeRoleId = 1;

    public EmployeeService(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
        Seed();
    }

    public IReadOnlyList<Employee> GetAll() => _employees.Values.OrderBy(e => e.EmployeeNo).ToList();

    public IReadOnlyList<Employee> Search(EmployeeFilter filter)
    {
        var query = _employees.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.EmployeeNo))
        {
            query = query.Where(e => e.EmployeeNo.Contains(filter.EmployeeNo.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(e => e.Name.Contains(filter.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrgId))
        {
            query = query.Where(e => string.Equals(e.OrgId, filter.OrgId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            query = query.Where(e => e.Title.Contains(filter.Title.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (filter.ManagerId.HasValue)
        {
            query = query.Where(e => e.ManagerId == filter.ManagerId);
        }

        return query.OrderBy(e => e.EmployeeNo).ToList();
    }

    public Employee? GetById(int id) => _employees.TryGetValue(id, out var employee) ? employee : null;

    public (bool success, string? error) Create(Employee employee)
    {
        var validation = Validate(employee, null);
        if (!validation.success)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(employee.EmployeeId))
        {
            employee.EmployeeId = Guid.NewGuid().ToString();
        }

        employee.CreatedAt = employee.CreatedAt == default ? DateTime.UtcNow : employee.CreatedAt;
        employee.UpdatedAt = null;
        employee.Id = Interlocked.Increment(ref _employeeId);
        employee.Roles = new List<EmployeeRole>();
        _employees.TryAdd(employee.Id, Clone(employee));
        return (true, null);
    }

    public (bool success, string? error) Update(int id, Employee updated)
    {
        if (!_employees.TryGetValue(id, out var existing))
        {
            return (false, "找不到員工");
        }

        var validation = Validate(updated, id);
        if (!validation.success)
        {
            return validation;
        }

        updated.EmployeeId = string.IsNullOrWhiteSpace(updated.EmployeeId) ? existing.EmployeeId : updated.EmployeeId;
        updated.CreatedAt = existing.CreatedAt;
        updated.UpdatedAt = DateTime.UtcNow;
        updated.Id = id;
        updated.Roles = existing.Roles;
        _employees[id] = Clone(updated);
        return (true, null);
    }

    public (bool success, string? error) Delete(int id)
    {
        var removed = _employees.TryRemove(id, out _);
        if (removed)
        {
            foreach (var employee in _employees.Values)
            {
                if (employee.ManagerId == id)
                {
                    employee.ManagerId = null;
                }
            }
        }

        return removed ? (true, null) : (false, "找不到員工");
    }

    public IReadOnlyList<EmployeeRole> GetRoles(int employeeId)
    {
        return _employees.TryGetValue(employeeId, out var employee)
            ? employee.Roles.OrderByDescending(r => r.IsPrimary).ThenBy(r => r.RoleName).ToList()
            : Array.Empty<EmployeeRole>();
    }

    public (bool success, string? error) AddRole(int employeeId, EmployeeRole role)
    {
        if (!_employees.TryGetValue(employeeId, out var employee))
        {
            return (false, "找不到員工");
        }

        if (string.IsNullOrWhiteSpace(role.RoleName))
        {
            return (false, "職能名稱必填");
        }

        var normalizedRoleName = role.RoleName.Trim();
        if (employee.Roles.Any(r => r.RoleName.Equals(normalizedRoleName, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "職能重複");
        }

        var newRole = new EmployeeRole
        {
            Id = Interlocked.Increment(ref _employeeRoleId),
            EmployeeId = employeeId,
            RoleCode = string.IsNullOrWhiteSpace(role.RoleCode) ? normalizedRoleName : role.RoleCode.Trim(),
            RoleName = normalizedRoleName,
            IsPrimary = role.IsPrimary,
            CreatedAt = DateTime.UtcNow
        };

        if (newRole.IsPrimary)
        {
            foreach (var existing in employee.Roles)
            {
                existing.IsPrimary = false;
            }
        }

        employee.Roles.Add(newRole);
        return (true, null);
    }

    public (bool success, string? error) RemoveRole(int employeeId, int roleId)
    {
        if (!_employees.TryGetValue(employeeId, out var employee))
        {
            return (false, "找不到員工");
        }

        var removed = employee.Roles.RemoveAll(r => r.Id == roleId) > 0;
        return removed ? (true, null) : (false, "找不到職能");
    }

    public (bool success, string? error) SetPrimaryRole(int employeeId, int roleId)
    {
        if (!_employees.TryGetValue(employeeId, out var employee))
        {
            return (false, "找不到員工");
        }

        var target = employee.Roles.FirstOrDefault(r => r.Id == roleId);
        if (target == null)
        {
            return (false, "找不到職能");
        }

        foreach (var role in employee.Roles)
        {
            role.IsPrimary = role.Id == roleId;
        }

        return (true, null);
    }

    private (bool success, string? error) Validate(Employee employee, int? updatingId)
    {
        if (string.IsNullOrWhiteSpace(employee.EmployeeNo))
        {
            return (false, "員工編號必填");
        }

        if (string.IsNullOrWhiteSpace(employee.Name))
        {
            return (false, "姓名必填");
        }

        if (string.IsNullOrWhiteSpace(employee.Status))
        {
            return (false, "狀態必填");
        }

        if (string.IsNullOrWhiteSpace(employee.OrgId) || !_organizationService.Exists(employee.OrgId))
        {
            return (false, "組織不存在");
        }

        if (_employees.Values.Any(e => e.EmployeeNo.Equals(employee.EmployeeNo.Trim(), StringComparison.OrdinalIgnoreCase) && e.Id != updatingId))
        {
            return (false, "員工編號重複");
        }

        if (!string.IsNullOrWhiteSpace(employee.Email) && _employees.Values.Any(e => string.Equals(e.Email, employee.Email.Trim(), StringComparison.OrdinalIgnoreCase) && e.Id != updatingId))
        {
            return (false, "Email 已存在");
        }

        if (employee.ManagerId.HasValue)
        {
            if (!_employees.ContainsKey(employee.ManagerId.Value))
            {
                return (false, "主管不存在");
            }

            if (updatingId.HasValue && employee.ManagerId.Value == updatingId.Value)
            {
                return (false, "主管不可為自己");
            }
        }

        return (true, null);
    }

    private static Employee Clone(Employee employee)
    {
        return new Employee
        {
            EmployeeId = employee.EmployeeId,
            Id = employee.Id,
            EmployeeNo = employee.EmployeeNo.Trim(),
            Name = employee.Name.Trim(),
            Email = employee.Email?.Trim(),
            Status = employee.Status.Trim(),
            OrgId = employee.OrgId.Trim(),
            Title = employee.Title.Trim(),
            ManagerId = employee.ManagerId,
            SupervisorId = employee.SupervisorId,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            Roles = employee.Roles.Select(r => new EmployeeRole
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                IsPrimary = r.IsPrimary,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    private void Seed()
    {
        var seedEmployees = new List<Employee>
        {
            new() { EmployeeNo = "EMP-001", Name = "王小明", Email = "ming.wang@example.com", OrgId = "PMO", Title = "專案經理" },
            new() { EmployeeNo = "EMP-002", Name = "林美美", Email = "mei.lin@example.com", OrgId = "RD-FE", Title = "資深工程師", ManagerId = 1001 },
            new() { EmployeeNo = "EMP-003", Name = "陳大華", Email = "dahua.chen@example.com", OrgId = "RD-BE", Title = "系統分析師", ManagerId = 1001 },
            new() { EmployeeNo = "EMP-004", Name = "劉文強", Email = "wen.liu@example.com", OrgId = "HR", Title = "人資主管" }
        };

        foreach (var seed in seedEmployees)
        {
            seed.Id = Interlocked.Increment(ref _employeeId);
            seed.Roles = new List<EmployeeRole>();
            _employees.TryAdd(seed.Id, Clone(seed));
        }

        // assign manager relationships after initial insert for clarity
        if (_employees.Values.FirstOrDefault(e => e.EmployeeNo == "EMP-001") is { } manager)
        {
            foreach (var employee in _employees.Values.Where(e => e.ManagerId == 1001))
            {
                employee.ManagerId = manager.Id;
            }
        }

        if (_employees.Values.FirstOrDefault(e => e.EmployeeNo == "EMP-001") is { } pm)
        {
            pm.Roles.Add(new EmployeeRole
            {
                Id = Interlocked.Increment(ref _employeeRoleId),
                EmployeeId = pm.Id,
                RoleName = "專案經理",
                IsPrimary = true
            });
        }
    }
}
