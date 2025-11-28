using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class OrganizationFilterViewModel
{
    public string? Keyword { get; set; }
    public bool ActiveOnly { get; set; }
}

public class OrganizationListItemViewModel
{
    public string OrgId { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? ParentOrgName { get; set; }
    public int? OrgLevel { get; set; }
    public string? PortfolioCode { get; set; }
    public bool IsActive { get; set; }
}

public class OrganizationFormViewModel
{
    [Required]
    [StringLength(50)]
    [Display(Name = "組織代碼")]
    public string OrgId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "組織名稱")]
    public string OrgName { get; set; } = string.Empty;

    [Display(Name = "上層組織")]
    public string? ParentOrgId { get; set; }

    [StringLength(50)]
    [Display(Name = "成本歸屬代碼")]
    public string? PortfolioCode { get; set; }

    [Range(1, 10, ErrorMessage = "層級需為正整數")]
    [Display(Name = "層級")]
    public int? OrgLevel { get; set; }

    [Display(Name = "是否啟用")]
    public bool IsActive { get; set; } = true;
}

public class OrganizationIndexPageViewModel
{
    public OrganizationFilterViewModel Filter { get; set; } = new();
    public List<OrganizationListItemViewModel> Items { get; set; } = new();
}

public class OrganizationDetailViewModel
{
    public string OrgId { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? ParentOrgName { get; set; }
    public string? PortfolioCode { get; set; }
    public int? OrgLevel { get; set; }
    public bool IsActive { get; set; }
    public int DirectChildCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
