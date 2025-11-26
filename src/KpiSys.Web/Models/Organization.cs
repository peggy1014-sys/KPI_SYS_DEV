using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class Organization
{
    [Required]
    [Display(Name = "組織代碼")]
    public string OrgId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "組織名稱")]
    public string OrgName { get; set; } = string.Empty;

    [Display(Name = "上層組織代碼")]
    public string? ParentOrgId { get; set; }

    [Range(0, 10, ErrorMessage = "層級需介於 0-10" )]
    [Display(Name = "層級")]
    public int OrgLevel { get; set; }
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
