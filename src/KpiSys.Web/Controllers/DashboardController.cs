using KpiSys.Web;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

[SessionAuthorize]
public class DashboardController : Controller
{
    [HttpGet]
    [SessionAuthorize("Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    [HttpGet]
    [SessionAuthorize("PM")]
    public IActionResult Pm()
    {
        return View();
    }

    [HttpGet]
    [SessionAuthorize("Employee")]
    public IActionResult Employee()
    {
        return View();
    }

    [HttpGet]
    [SessionAuthorize("Manager")]
    public IActionResult Manager()
    {
        return View();
    }
}
