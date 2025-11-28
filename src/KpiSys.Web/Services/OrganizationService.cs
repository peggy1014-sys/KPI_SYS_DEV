using System;
using System.Collections.Concurrent;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface IOrganizationService
{
    IReadOnlyList<Organization> GetAll();
    IReadOnlyList<OrganizationNode> GetTree();
    Organization? GetById(string orgId);
    (bool success, string? error) Add(Organization organization);
    (bool success, string? error) Update(string orgId, Organization updated);
    (bool success, string? error) Delete(string orgId);
    bool Exists(string orgId);
}

public class OrganizationService : IOrganizationService
{
    private readonly ConcurrentDictionary<string, Organization> _organizations;

    public OrganizationService()
    {
        _organizations = new ConcurrentDictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);
        Seed();
    }

    public IReadOnlyList<Organization> GetAll()
    {
        return _organizations.Values
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .ToList();
    }

    public IReadOnlyList<OrganizationNode> GetTree()
    {
        var lookup = _organizations.Values.ToDictionary(o => o.OrgId, StringComparer.OrdinalIgnoreCase);
        var nodeLookup = lookup.Values.ToDictionary(o => o.OrgId, o => new OrganizationNode { Node = o }, StringComparer.OrdinalIgnoreCase);

        foreach (var org in lookup.Values)
        {
            if (!string.IsNullOrWhiteSpace(org.ParentOrgId) && nodeLookup.TryGetValue(org.ParentOrgId, out var parent))
            {
                parent.Children.Add(nodeLookup[org.OrgId]);
            }
        }

        return nodeLookup.Values
            .Where(n => string.IsNullOrWhiteSpace(n.Node.ParentOrgId))
            .OrderBy(n => n.Node.OrgId)
            .ToList();
    }

    public Organization? GetById(string orgId)
    {
        return _organizations.TryGetValue(orgId, out var org) ? org : null;
    }

    public (bool success, string? error) Add(Organization organization)
    {
        if (string.IsNullOrWhiteSpace(organization.OrgId))
        {
            return (false, "orgId 必填");
        }

        if (string.IsNullOrWhiteSpace(organization.OrgName))
        {
            return (false, "orgName 必填");
        }

        if (!string.IsNullOrWhiteSpace(organization.ParentOrgId) && !_organizations.ContainsKey(organization.ParentOrgId))
        {
            return (false, "上層組織不存在");
        }

        if (!string.IsNullOrWhiteSpace(organization.OrgCode) &&
            _organizations.Values.Any(o => string.Equals(o.OrgCode, organization.OrgCode.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "orgCode 重複");
        }

        if (organization.OrgLevel <= 0)
        {
            organization.OrgLevel = CalculateLevel(organization.ParentOrgId);
        }

        organization.CreatedAt = organization.CreatedAt == default ? DateTime.UtcNow : organization.CreatedAt;
        organization.UpdatedAt = null;
        var added = _organizations.TryAdd(organization.OrgId.Trim(), Normalize(organization));
        return added ? (true, null) : (false, "orgId 重複");
    }

    public (bool success, string? error) Update(string orgId, Organization updated)
    {
        if (!_organizations.TryGetValue(orgId, out var existing))
        {
            return (false, "找不到組織");
        }

        if (!string.IsNullOrWhiteSpace(updated.ParentOrgId) && !_organizations.ContainsKey(updated.ParentOrgId))
        {
            return (false, "上層組織不存在");
        }

        if (!string.IsNullOrWhiteSpace(updated.OrgCode) &&
            _organizations.Values.Any(o => string.Equals(o.OrgCode, updated.OrgCode.Trim(), StringComparison.OrdinalIgnoreCase) && !string.Equals(o.OrgId, orgId, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "orgCode 重複");
        }

        var normalized = Normalize(updated);
        normalized.OrgId = existing.OrgId;
        normalized.OrgLevel = updated.OrgLevel > 0 ? updated.OrgLevel : CalculateLevel(updated.ParentOrgId);
        normalized.CreatedAt = existing.CreatedAt;
        normalized.UpdatedAt = DateTime.UtcNow;
        _organizations[orgId] = normalized;
        return (true, null);
    }

    public (bool success, string? error) Delete(string orgId)
    {
        var hasChildren = _organizations.Values.Any(o => string.Equals(o.ParentOrgId, orgId, StringComparison.OrdinalIgnoreCase));
        if (hasChildren)
        {
            return (false, "請先移除子節點");
        }

        var removed = _organizations.TryRemove(orgId, out _);
        return removed ? (true, null) : (false, "找不到組織");
    }

    public bool Exists(string orgId) => _organizations.ContainsKey(orgId);

    private int CalculateLevel(string? parentOrgId)
    {
        if (string.IsNullOrWhiteSpace(parentOrgId))
        {
            return 1;
        }

        return _organizations.TryGetValue(parentOrgId, out var parent)
            ? parent.OrgLevel + 1
            : 1;
    }

    private static Organization Normalize(Organization org)
    {
        return new Organization
        {
            OrgId = org.OrgId.Trim(),
            OrgName = org.OrgName.Trim(),
            OrgCode = string.IsNullOrWhiteSpace(org.OrgCode) ? null : org.OrgCode.Trim(),
            ParentOrgId = string.IsNullOrWhiteSpace(org.ParentOrgId) ? null : org.ParentOrgId.Trim(),
            OrgLevel = org.OrgLevel,
            PortfolioCode = string.IsNullOrWhiteSpace(org.PortfolioCode) ? null : org.PortfolioCode.Trim(),
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }

    private void Seed()
    {
        var seedData = new List<Organization>
        {
            new() { OrgId = "HQ", OrgName = "總公司", OrgLevel = 1 },
            new() { OrgId = "RD", OrgName = "研發處", ParentOrgId = "HQ" },
            new() { OrgId = "PMO", OrgName = "專案管理辦公室", ParentOrgId = "HQ" },
            new() { OrgId = "HR", OrgName = "人資行政", ParentOrgId = "HQ" },
            new() { OrgId = "RD-FE", OrgName = "前端組", ParentOrgId = "RD" },
            new() { OrgId = "RD-BE", OrgName = "後端組", ParentOrgId = "RD" },
        };

        foreach (var item in seedData)
        {
            item.OrgLevel = CalculateLevel(item.ParentOrgId);
            _organizations.TryAdd(item.OrgId, Normalize(item));
        }
    }
}
