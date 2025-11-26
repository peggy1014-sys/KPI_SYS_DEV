using KpiSys.Web;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _userService.Authenticate(model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
            return View(model);
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.UserName, user.Name);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.Role);

        TempData["Message"] = $"歡迎回來，{user.Name}";
        return RedirectToAction("Index", "Home");
    }

    [SessionAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Forbidden()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
