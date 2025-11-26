namespace KpiSys.Web.Models;

public class OrganizationTreeNodeViewModel
{
    public Organization Organization { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public List<OrganizationTreeNodeViewModel> Children { get; set; } = new();
}

public class OrganizationBrowserViewModel
{
    public List<OrganizationTreeNodeViewModel> Tree { get; set; } = new();
    public int TotalOrganizations { get; set; }
    public int TotalEmployees { get; set; }
}
