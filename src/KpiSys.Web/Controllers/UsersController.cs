using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize("Admin")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmployeeService _employeeService;

    public UsersController(IUserService userService, IEmployeeService employeeService)
    {
        _userService = userService;
        _employeeService = employeeService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new UserListViewModel
        {
            Users = _userService.GetAll().ToList(),
            Employees = _employeeService.GetAll().ToList(),
            NewUser = new UserAccount()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(UserAccount user)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", BuildViewModel(user));
        }

        var (success, error) = _userService.Create(user);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "新增失敗");
            return View("Index", BuildViewModel(user));
        }

        TempData["Message"] = "使用者已新增";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var user = _userService.GetById(id);
        if (user == null)
        {
            return NotFound();
        }

        ViewBag.Employees = _employeeService.GetAll();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, UserAccount user)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Employees = _employeeService.GetAll();
            return View(user);
        }

        var (success, error) = _userService.Update(id, user);
        if (!success)
        {
            ViewBag.Employees = _employeeService.GetAll();
            ModelState.AddModelError(string.Empty, error ?? "更新失敗");
            return View(user);
        }

        TempData["Message"] = "使用者已更新";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var (success, error) = _userService.Delete(id);
        TempData["Message"] = success ? "使用者已刪除" : error ?? "刪除失敗";
        return RedirectToAction(nameof(Index));
    }

    private UserListViewModel BuildViewModel(UserAccount form)
    {
        return new UserListViewModel
        {
            Users = _userService.GetAll().ToList(),
            Employees = _employeeService.GetAll().ToList(),
            NewUser = form
        };
    }
}
