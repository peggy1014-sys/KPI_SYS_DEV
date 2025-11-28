using System;
using System.Collections.Generic;
using System.Linq;
using KpiSys.Web.Data;
using KpiSys.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class OrganizationBrowserController : Controller
{
    private readonly KpiSysDbContext _db;

    public OrganizationBrowserController(KpiSysDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var organizations = await _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .ToListAsync();

        var employeesByOrg = await _db.Employees
            .AsNoTracking()
            .GroupBy(e => e.OrgId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.OrderBy(e => e.EmployeeNo)
                    .Select(e => new Employee
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeNo = e.EmployeeNo,
                        Name = e.EmployeeName,
                        OrgId = e.OrgId,
                        Title = e.Status,
                        Email = e.Email,
                        Status = e.Status,
                        SupervisorId = e.SupervisorId,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt
                    })
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var tree = BuildTree(organizations, employeesByOrg);

        var model = new OrganizationBrowserViewModel
        {
            Tree = tree,
            TotalOrganizations = organizations.Count,
            TotalEmployees = employeesByOrg.Sum(g => g.Value.Count)
        };

        return View(model);
    }

    private static List<OrganizationTreeNodeViewModel> BuildTree(
        IReadOnlyList<Data.Entities.OrganizationEntity> organizations,
        IDictionary<string, List<Employee>> employeesByOrg)
    {
        var nodeLookup = organizations.ToDictionary(
            o => o.OrgId,
            o => new OrganizationTreeNodeViewModel
            {
                Organization = new Organization
                {
                    OrgId = o.OrgId,
                    OrgName = o.OrgName,
                    ParentOrgId = o.ParentOrgId,
                    PortfolioCode = o.PortfolioCode,
                    OrgLevel = o.OrgLevel,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                }
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var org in organizations)
        {
            if (!string.IsNullOrWhiteSpace(org.ParentOrgId) && nodeLookup.TryGetValue(org.ParentOrgId, out var parent))
            {
                parent.Children.Add(nodeLookup[org.OrgId]);
            }
        }

        foreach (var node in nodeLookup.Values)
        {
            node.Children = node.Children.OrderBy(c => c.Organization.OrgLevel).ThenBy(c => c.Organization.OrgId).ToList();
            node.Employees = employeesByOrg.TryGetValue(node.Organization.OrgId, out var empList)
                ? empList
                : new List<Employee>();
        }

        return nodeLookup.Values
            .Where(n => string.IsNullOrWhiteSpace(n.Organization.ParentOrgId))
            .OrderBy(n => n.Organization.OrgLevel)
            .ThenBy(n => n.Organization.OrgId)
            .ToList();
    }
}
