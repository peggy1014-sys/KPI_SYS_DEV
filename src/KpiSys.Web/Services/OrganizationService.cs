using System;
using System.Collections.Generic;
using KpiSys.Web.Data;
using KpiSys.Web.Data.Entities;
using KpiSys.Web.Models;
using Microsoft.EntityFrameworkCore;

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
    private readonly KpiSysDbContext _db;

    public OrganizationService(KpiSysDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<Organization> GetAll()
    {
        return _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .Select(Map)
            .ToList();
    }

    public IReadOnlyList<OrganizationNode> GetTree()
    {
        var lookup = GetAll().ToDictionary(o => o.OrgId, StringComparer.OrdinalIgnoreCase);
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
        var entity = _db.Organizations.AsNoTracking().FirstOrDefault(o => o.OrgId == orgId);
        return entity == null ? null : Map(entity);
    }

    public (bool success, string? error) Add(Organization organization)
    {
        var validation = Validate(organization, false);
        if (!validation.success)
        {
            return validation;
        }

        var normalized = Normalize(organization);
        normalized.OrgLevel = normalized.OrgLevel.HasValue && normalized.OrgLevel.Value > 0
            ? normalized.OrgLevel
            : CalculateLevel(normalized.ParentOrgId);

        var entity = new OrganizationEntity
        {
            OrgId = normalized.OrgId,
            OrgName = normalized.OrgName,
            ParentOrgId = normalized.ParentOrgId,
            PortfolioCode = normalized.PortfolioCode,
            OrgLevel = normalized.OrgLevel,
            IsActive = normalized.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _db.Organizations.Add(entity);
        _db.SaveChanges();
        return (true, null);
    }

    public (bool success, string? error) Update(string orgId, Organization updated)
    {
        var existing = _db.Organizations.FirstOrDefault(o => o.OrgId == orgId);
        if (existing == null)
        {
            return (false, "找不到組織");
        }

        var validation = Validate(updated, true, orgId);
        if (!validation.success)
        {
            return validation;
        }

        var normalized = Normalize(updated);
        normalized.OrgLevel = normalized.OrgLevel.HasValue && normalized.OrgLevel.Value > 0
            ? normalized.OrgLevel
            : CalculateLevel(normalized.ParentOrgId);

        existing.OrgName = normalized.OrgName;
        existing.ParentOrgId = normalized.ParentOrgId;
        existing.PortfolioCode = normalized.PortfolioCode;
        existing.OrgLevel = normalized.OrgLevel;
        existing.IsActive = normalized.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        _db.SaveChanges();
        return (true, null);
    }

    public (bool success, string? error) Delete(string orgId)
    {
        var entity = _db.Organizations.FirstOrDefault(o => o.OrgId == orgId);
        if (entity == null)
        {
            return (false, "找不到組織");
        }

        if (_db.Organizations.Any(o => o.ParentOrgId == orgId))
        {
            return (false, "請先移除子節點");
        }

        // TODO: 檢查是否仍有員工隸屬於此組織，避免刪除後造成孤兒資料

        _db.Organizations.Remove(entity);
        _db.SaveChanges();
        return (true, null);
    }

    public bool Exists(string orgId) => _db.Organizations.Any(o => o.OrgId == orgId);

    private int CalculateLevel(string? parentOrgId)
    {
        if (string.IsNullOrWhiteSpace(parentOrgId))
        {
            return 1;
        }

        var parent = _db.Organizations.AsNoTracking().FirstOrDefault(o => o.OrgId == parentOrgId);
        return parent?.OrgLevel.HasValue == true ? parent.OrgLevel.Value + 1 : 1;
    }

    private static Organization Normalize(Organization org)
    {
        return new Organization
        {
            OrgId = org.OrgId.Trim(),
            OrgName = org.OrgName.Trim(),
            ParentOrgId = string.IsNullOrWhiteSpace(org.ParentOrgId) ? null : org.ParentOrgId.Trim(),
            OrgLevel = org.OrgLevel,
            PortfolioCode = string.IsNullOrWhiteSpace(org.PortfolioCode) ? null : org.PortfolioCode.Trim(),
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }

    private (bool success, string? error) Validate(Organization organization, bool isUpdate, string? currentOrgId = null)
    {
        if (string.IsNullOrWhiteSpace(organization.OrgId))
        {
            return (false, "orgId 必填");
        }

        if (string.IsNullOrWhiteSpace(organization.OrgName))
        {
            return (false, "orgName 必填");
        }

        if (organization.OrgLevel.HasValue && organization.OrgLevel <= 0)
        {
            return (false, "層級需為正整數");
        }

        var normalizedId = organization.OrgId.Trim();
        var normalizedParentId = string.IsNullOrWhiteSpace(organization.ParentOrgId)
            ? null
            : organization.ParentOrgId.Trim();

        if (normalizedParentId != null && string.Equals(normalizedId, normalizedParentId, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "上層組織不可為自己");
        }

        if (!isUpdate && _db.Organizations.AsNoTracking().Any(o => o.OrgId == normalizedId))
        {
            return (false, "orgId 重複");
        }

        if (normalizedParentId != null && !_db.Organizations.AsNoTracking().Any(o => o.OrgId == normalizedParentId))
        {
            return (false, "上層組織不存在");
        }

        if (isUpdate && normalizedParentId != null && IsCircularParent(normalizedId, normalizedParentId))
        {
            return (false, "上層組織不可形成循環");
        }

        return (true, null);
    }

    private bool IsCircularParent(string orgId, string? parentOrgId)
    {
        var currentParentId = parentOrgId;
        while (!string.IsNullOrWhiteSpace(currentParentId))
        {
            if (string.Equals(currentParentId, orgId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            currentParentId = _db.Organizations
                .AsNoTracking()
                .Where(o => o.OrgId == currentParentId)
                .Select(o => o.ParentOrgId)
                .FirstOrDefault();
        }

        return false;
    }

    private static Organization Map(OrganizationEntity entity)
    {
        return new Organization
        {
            OrgId = entity.OrgId,
            OrgName = entity.OrgName,
            ParentOrgId = entity.ParentOrgId,
            PortfolioCode = entity.PortfolioCode,
            OrgLevel = entity.OrgLevel,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
