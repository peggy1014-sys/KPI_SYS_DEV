using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class OrganizationsController : Controller
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new OrganizationIndexViewModel
        {
            Tree = _organizationService.GetTree().ToList(),
            NewOrganization = new Organization()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Organization organization)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new OrganizationIndexViewModel
            {
                Tree = _organizationService.GetTree().ToList(),
                NewOrganization = organization
            });
        }

        var (success, error) = _organizationService.Add(organization);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增失敗");
            return View("Index", new OrganizationIndexViewModel
            {
                Tree = _organizationService.GetTree().ToList(),
                NewOrganization = organization
            });
        }

        TempData["Message"] = "組織已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(string id)
    {
        var org = _organizationService.GetById(id);
        if (org == null)
        {
            return NotFound();
        }

        ViewBag.Organizations = _organizationService.GetAll();
        return View(org);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(string id, Organization organization)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Organizations = _organizationService.GetAll();
            return View(organization);
        }

        var (success, error) = _organizationService.Update(id, organization);
        if (!success)
        {
            ViewBag.Organizations = _organizationService.GetAll();
            ModelState.AddModelError(string.Empty, error ?? "更新失敗");
            return View(organization);
        }

        TempData["Message"] = "組織已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        var (success, error) = _organizationService.Delete(id);
        TempData["Message"] = success ? "組織已刪除" : error ?? "刪除失敗";
        return RedirectToAction(nameof(Index));
    }
}
