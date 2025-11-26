using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "員工編號")]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "電子郵件")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "組織代碼")]
    public string OrgId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "職稱")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "主管員編")]
    public int? ManagerId { get; set; }

    public List<EmployeeRole> Roles { get; set; } = new();
}

public class EmployeeRole
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "職能名稱")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "主要職能")]
    public bool IsPrimary { get; set; }
}

public class EmployeeFilter
{
    public string? EmployeeNo { get; set; }
    public string? Name { get; set; }
    public string? OrgId { get; set; }
    public string? Title { get; set; }
    public int? ManagerId { get; set; }
}

public class EmployeeSummaryViewModel
{
    public int Id { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Manager { get; set; }
}

public class EmployeeListViewModel
{
    public EmployeeFilter Filter { get; set; } = new();
    public IReadOnlyList<EmployeeSummaryViewModel> Employees { get; set; } = new List<EmployeeSummaryViewModel>();
    public int TotalEmployees { get; set; }
    public int DepartmentCount { get; set; }
    public int SearchResultCount { get; set; }
    public List<Organization> Organizations { get; set; } = new();
    public List<Employee> Managers { get; set; } = new();
}

public class EmployeeFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "員工編號")]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "電子郵件")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "組織代碼")]
    public string OrgId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "職稱")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "主管員編")]
    public int? ManagerId { get; set; }

    public List<Organization> Organizations { get; set; } = new();
    public List<Employee> Managers { get; set; } = new();
}

public class EmployeeDetailViewModel
{
    public Employee Employee { get; set; } = new();
    public string OrganizationName { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public EmployeeRole NewRole { get; set; } = new();
}
