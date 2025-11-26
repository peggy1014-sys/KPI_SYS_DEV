using System.Collections.Generic;
using System.Linq;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class OrganizationBrowserController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly IEmployeeService _employeeService;

    public OrganizationBrowserController(IOrganizationService organizationService, IEmployeeService employeeService)
    {
        _organizationService = organizationService;
        _employeeService = employeeService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var employeesByOrg = _employeeService.GetAll()
            .GroupBy(e => e.OrgId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.EmployeeNo).ToList(), StringComparer.OrdinalIgnoreCase);

        var tree = BuildTree(_organizationService.GetTree(), employeesByOrg);

        var model = new OrganizationBrowserViewModel
        {
            Tree = tree,
            TotalOrganizations = _organizationService.GetAll().Count,
            TotalEmployees = _employeeService.GetAll().Count
        };

        return View(model);
    }

    private static List<OrganizationTreeNodeViewModel> BuildTree(
        IReadOnlyList<OrganizationNode> nodes,
        IDictionary<string, List<Employee>> employeesByOrg)
    {
        var result = new List<OrganizationTreeNodeViewModel>();

        foreach (var node in nodes.OrderBy(n => n.Node.OrgLevel).ThenBy(n => n.Node.OrgId))
        {
            var viewNode = new OrganizationTreeNodeViewModel
            {
                Organization = node.Node,
                Employees = employeesByOrg.TryGetValue(node.Node.OrgId, out var empList)
                    ? empList
                    : new List<Employee>(),
                Children = BuildTree(node.Children, employeesByOrg)
            };

            result.Add(viewNode);
        }

        return result;
    }
}
