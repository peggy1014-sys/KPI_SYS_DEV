using System.ComponentModel.DataAnnotations;
using System;

namespace KpiSys.Web.Models;

public class Organization
{
    [Required]
    [Display(Name = "組織代碼")]
    [StringLength(50)]
    public string OrgId { get; set; } = string.Empty;

    [Display(Name = "組織匯入代碼")]
    public string? OrgCode { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "組織名稱")]
    public string OrgName { get; set; } = string.Empty;

    [Display(Name = "上層組織代碼")]
    public string? ParentOrgId { get; set; }

    [Range(1, 10, ErrorMessage = "層級需為正整數")]
    [Display(Name = "層級")]
    public int? OrgLevel { get; set; }

    [Display(Name = "事業群代碼")]
    [StringLength(50)]
    public string? PortfolioCode { get; set; }

    [Display(Name = "是否啟用")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "建立時間")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "更新時間")]
    public DateTime? UpdatedAt { get; set; }
}

public class OrganizationNode
{
    public Organization Node { get; set; } = new();
    public List<OrganizationNode> Children { get; set; } = new();
}

public class OrganizationIndexViewModel
{
    public List<OrganizationNode> Tree { get; set; } = new();
    public Organization NewOrganization { get; set; } = new();
}
