using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers
{
    public class EmployeesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
