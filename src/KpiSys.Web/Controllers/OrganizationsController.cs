using System;
using KpiSys.Web;
using KpiSys.Web.Data;
using KpiSys.Web.Data.Entities;
using KpiSys.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class OrganizationsController : Controller
{
    private readonly KpiSysDbContext _db;

    public OrganizationsController(KpiSysDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, bool activeOnly = false)
    {
        var query = _db.Organizations
            .Include(o => o.Parent)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(o => EF.Functions.Like(o.OrgName, $"%{keyword.Trim()}%"));
        }

        if (activeOnly)
        {
            query = query.Where(o => o.IsActive);
        }

        var childCounts = await _db.Organizations
            .GroupBy(o => o.ParentOrgId)
            .Select(g => new { ParentOrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ParentOrgId, g => g.Count);

        var items = await query
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .Select(o => new OrganizationListItemViewModel
            {
                OrgId = o.OrgId,
                OrgName = o.OrgName,
                ParentOrgName = o.Parent != null ? o.Parent.OrgName : null,
                OrgLevel = o.OrgLevel,
                PortfolioCode = o.PortfolioCode,
                IsActive = o.IsActive,
                HasChildren = childCounts.ContainsKey(o.OrgId) && childCounts[o.OrgId] > 0
            })
            .ToListAsync();

        var model = new OrganizationIndexPageViewModel
        {
            Filter = new OrganizationFilterViewModel
            {
                Keyword = keyword,
                ActiveOnly = activeOnly
            },
            Items = items
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateParentSelectList();
        return View(new OrganizationFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganizationFormViewModel model)
    {
        await ValidateParentAsync(model);

        if (await _db.Organizations.AnyAsync(o => o.OrgId == model.OrgId))
        {
            ModelState.AddModelError(nameof(model.OrgId), "組織代碼已存在");
        }

        if (!ModelState.IsValid)
        {
            await PopulateParentSelectList();
            return View(model);
        }

        if (!model.OrgLevel.HasValue)
        {
            model.OrgLevel = await GetLevelFromParent(model.ParentOrgId);
        }

        var entity = new OrganizationEntity
        {
            OrgId = model.OrgId,
            OrgName = model.OrgName,
            ParentOrgId = string.IsNullOrWhiteSpace(model.ParentOrgId) ? null : model.ParentOrgId,
            PortfolioCode = string.IsNullOrWhiteSpace(model.PortfolioCode) ? null : model.PortfolioCode,
            OrgLevel = model.OrgLevel,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Organizations.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Message"] = "組織已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.OrgId == id);
        if (org == null)
        {
            return NotFound();
        }

        var model = new OrganizationFormViewModel
        {
            OrgId = org.OrgId,
            OrgName = org.OrgName,
            ParentOrgId = org.ParentOrgId,
            PortfolioCode = org.PortfolioCode,
            OrgLevel = org.OrgLevel,
            IsActive = org.IsActive
        };

        await PopulateParentSelectList(id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, OrganizationFormViewModel model)
    {
        if (!string.Equals(id, model.OrgId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        await ValidateParentAsync(model, id);

        if (!ModelState.IsValid)
        {
            await PopulateParentSelectList(id);
            return View(model);
        }

        if (!model.OrgLevel.HasValue)
        {
            model.OrgLevel = await GetLevelFromParent(model.ParentOrgId);
        }

        var entity = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgId == id);
        if (entity == null)
        {
            return NotFound();
        }

        entity.OrgName = model.OrgName;
        entity.ParentOrgId = string.IsNullOrWhiteSpace(model.ParentOrgId) ? null : model.ParentOrgId;
        entity.PortfolioCode = string.IsNullOrWhiteSpace(model.PortfolioCode) ? null : model.PortfolioCode;
        entity.OrgLevel = model.OrgLevel;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Message"] = "組織已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var org = await _db.Organizations
            .Include(o => o.Parent)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrgId == id);

        if (org == null)
        {
            return NotFound();
        }

        var childCount = await _db.Organizations.CountAsync(o => o.ParentOrgId == id);

        var model = new OrganizationDetailViewModel
        {
            OrgId = org.OrgId,
            OrgName = org.OrgName,
            ParentOrgName = org.Parent?.OrgName,
            PortfolioCode = org.PortfolioCode,
            OrgLevel = org.OrgLevel,
            IsActive = org.IsActive,
            DirectChildCount = childCount,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var org = await _db.Organizations
            .Include(o => o.Parent)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrgId == id);

        if (org == null)
        {
            return NotFound();
        }

        var childCount = await _db.Organizations.CountAsync(o => o.ParentOrgId == id);

        var model = new OrganizationDetailViewModel
        {
            OrgId = org.OrgId,
            OrgName = org.OrgName,
            ParentOrgName = org.Parent?.OrgName,
            PortfolioCode = org.PortfolioCode,
            OrgLevel = org.OrgLevel,
            IsActive = org.IsActive,
            DirectChildCount = childCount,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var hasChildren = await _db.Organizations.AnyAsync(o => o.ParentOrgId == id);
        if (hasChildren)
        {
            TempData["Message"] = "此組織有下層單位，無法刪除";
            return RedirectToAction(nameof(Index));
        }

        var entity = await _db.Organizations.FirstOrDefaultAsync(o => o.OrgId == id);
        if (entity == null)
        {
            TempData["Message"] = "找不到組織";
            return RedirectToAction(nameof(Index));
        }

        _db.Organizations.Remove(entity);
        await _db.SaveChangesAsync();
        TempData["Message"] = "組織已刪除";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentSelectList(string? excludeOrgId = null)
    {
        var organizations = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(excludeOrgId))
        {
            organizations = organizations
                .Where(o => !string.Equals(o.OrgId, excludeOrgId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.ParentOrganizations = organizations;
    }

    private async Task ValidateParentAsync(OrganizationFormViewModel model, string? currentOrgId = null)
    {
        if (string.IsNullOrWhiteSpace(model.OrgId))
        {
            ModelState.AddModelError(nameof(model.OrgId), "組織代碼必填");
        }

        if (model.ParentOrgId != null && string.Equals(model.ParentOrgId, model.OrgId, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.ParentOrgId), "上層組織不可為自己");
        }

        if (!string.IsNullOrWhiteSpace(model.ParentOrgId))
        {
            var parentExists = await _db.Organizations.AsNoTracking().AnyAsync(o => o.OrgId == model.ParentOrgId);
            if (!parentExists)
            {
                ModelState.AddModelError(nameof(model.ParentOrgId), "上層組織不存在");
            }
            else if (!string.IsNullOrWhiteSpace(currentOrgId) && await IsCircularParentAsync(currentOrgId, model.ParentOrgId))
            {
                ModelState.AddModelError(nameof(model.ParentOrgId), "上層組織不可形成循環");
            }
        }

    }

    private async Task<int?> GetLevelFromParent(string? parentOrgId)
    {
        if (string.IsNullOrWhiteSpace(parentOrgId))
        {
            return 1;
        }

        var parentLevel = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.OrgId == parentOrgId)
            .Select(o => o.OrgLevel)
            .FirstOrDefaultAsync();

        return parentLevel.HasValue ? parentLevel + 1 : 1;
    }

    private async Task<bool> IsCircularParentAsync(string orgId, string? parentOrgId)
    {
        var currentParentId = parentOrgId;
        while (!string.IsNullOrWhiteSpace(currentParentId))
        {
            if (string.Equals(currentParentId, orgId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            currentParentId = await _db.Organizations
                .AsNoTracking()
                .Where(o => o.OrgId == currentParentId)
                .Select(o => o.ParentOrgId)
                .FirstOrDefaultAsync();
        }

        return false;
    }
}
