using KpiSys.Web;
using KpiSys.Web.Data;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class OrganizationsController : Controller
{
    private readonly KpiSysDbContext _db;
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(KpiSysDbContext db, IOrganizationService organizationService)
    {
        _db = db;
        _organizationService = organizationService;
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
                IsActive = o.IsActive
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

        if (!ModelState.IsValid)
        {
            await PopulateParentSelectList();
            return View(model);
        }

        if (!model.OrgLevel.HasValue && !string.IsNullOrWhiteSpace(model.ParentOrgId))
        {
            model.OrgLevel = await GetLevelFromParent(model.ParentOrgId);
        }

        var (success, error) = _organizationService.Add(new Organization
        {
            OrgId = model.OrgId,
            OrgName = model.OrgName,
            ParentOrgId = model.ParentOrgId,
            PortfolioCode = model.PortfolioCode,
            OrgLevel = model.OrgLevel,
            IsActive = model.IsActive
        });

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增失敗");
            await PopulateParentSelectList();
            return View(model);
        }

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

        if (!model.OrgLevel.HasValue && !string.IsNullOrWhiteSpace(model.ParentOrgId))
        {
            model.OrgLevel = await GetLevelFromParent(model.ParentOrgId);
        }

        var (success, error) = _organizationService.Update(id, new Organization
        {
            OrgId = model.OrgId,
            OrgName = model.OrgName,
            ParentOrgId = model.ParentOrgId,
            PortfolioCode = model.PortfolioCode,
            OrgLevel = model.OrgLevel,
            IsActive = model.IsActive
        });

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "更新失敗");
            await PopulateParentSelectList(id);
            return View(model);
        }

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
    public IActionResult DeleteConfirmed(string id)
    {
        var (success, error) = _organizationService.Delete(id);
        TempData["Message"] = success ? "組織已刪除" : error ?? "刪除失敗";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParentSelectList(string? excludeOrgId = null)
    {
        var organizations = await _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.OrgLevel)
            .ThenBy(o => o.OrgId)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(excludeOrgId))
        {
            organizations = organizations.Where(o => !string.Equals(o.OrgId, excludeOrgId, StringComparison.OrdinalIgnoreCase)).ToList();
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
        }

    }

    private async Task<int?> GetLevelFromParent(string parentOrgId)
    {
        var parentLevel = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.OrgId == parentOrgId)
            .Select(o => o.OrgLevel)
            .FirstOrDefaultAsync();

        return parentLevel.HasValue ? parentLevel + 1 : 1;
    }
}
