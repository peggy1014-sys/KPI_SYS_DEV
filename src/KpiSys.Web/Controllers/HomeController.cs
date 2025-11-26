using System.Diagnostics;
using KpiSys.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KpiSys.Web.Models;

namespace KpiSys.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [SessionAuthorize]
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString(SessionKeys.UserRole);

        return role switch
        {
            "Admin" => RedirectToAction("Admin", "Dashboard"),
            "PM" => RedirectToAction("Pm", "Dashboard"),
            "Manager" => RedirectToAction("Manager", "Dashboard"),
            "Employee" => RedirectToAction("Employee", "Dashboard"),
            _ => RedirectToAction("Login", "Auth")
        };
    }

    [SessionAuthorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
