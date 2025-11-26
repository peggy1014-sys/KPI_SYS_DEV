using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "專案代碼")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案名稱")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案類型")]
    public string ProjectType { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案規模")]
    public string ProjectSize { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案關鍵度")]
    public string ProjectCriticality { get; set; } = string.Empty;

    [Display(Name = "Portfolio")]
    public string? Portfolio { get; set; }

    [Display(Name = "PM")]
    public int? PmId { get; set; }

    [Display(Name = "開始日期")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "結束日期")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Required]
    [Display(Name = "狀態")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "專案說明")]
    public string? Description { get; set; }
}

public class ProjectMember
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal AllocationPct { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
}

public class ProjectMemberInput
{
    [Required]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "員工")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "角色")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "分配比例")]
    [Range(0, 100, ErrorMessage = "分配比例需介於 0 到 100 之間")]
    public decimal AllocationPct { get; set; } = 100;

    [DataType(DataType.Date)]
    [Display(Name = "生效日期")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "失效日期")]
    public DateTime? EndDate { get; set; }
}

public class ProjectMemberViewModel
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal AllocationPct { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectFilter
{
    public string? Keyword { get; set; }
    public string? ProjectType { get; set; }
    public string? Status { get; set; }
    public int? PmId { get; set; }
}

public class ProjectSummaryViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string? PmName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProjectListViewModel
{
    public ProjectFilter Filter { get; set; } = new();
    public IReadOnlyList<ProjectSummaryViewModel> Projects { get; set; } = Array.Empty<ProjectSummaryViewModel>();
    public IReadOnlyCollection<CodeItem> ProjectTypes { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> Statuses { get; set; } = Array.Empty<CodeItem>();
    public List<Employee> Employees { get; set; } = new();
}

public class ProjectFormViewModel
{
    public string? OriginalCode { get; set; }

    [Required]
    [Display(Name = "專案代碼")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案名稱")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案類型")]
    public string ProjectType { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案規模")]
    public string ProjectSize { get; set; } = string.Empty;

    [Required]
    [Display(Name = "專案關鍵度")]
    public string ProjectCriticality { get; set; } = string.Empty;

    [Display(Name = "Portfolio")]
    public string? Portfolio { get; set; }

    [Display(Name = "PM")]
    public int? PmId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "開始日期")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "結束日期")]
    public DateTime? EndDate { get; set; }

    [Required]
    [Display(Name = "狀態")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "專案說明")]
    public string? Description { get; set; }

    public IReadOnlyCollection<CodeItem> ProjectTypes { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> ProjectSizes { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> ProjectCriticalities { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> Portfolios { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> Statuses { get; set; } = Array.Empty<CodeItem>();
    public IReadOnlyCollection<CodeItem> TaskGroups { get; set; } = Array.Empty<CodeItem>();
    public List<Employee> Employees { get; set; } = new();
    public List<ProjectMemberViewModel> Members { get; set; } = new();
    public ProjectMemberInput NewMember { get; set; } = new();
    public List<ProjectTaskViewModel> Tasks { get; set; } = new();
    public ProjectTaskInput NewTask { get; set; } = new();
}
