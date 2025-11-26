using KpiSys.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers
{
    public class EmployeesController : Controller
    {
        private static readonly List<EmployeeViewModel> Employees = new()
        {
            new EmployeeViewModel { EmployeeNo = "E001", Name = "王小明", Email = "ming.wang@example.com", Organization = "研發部", Title = "資深工程師" },
            new EmployeeViewModel { EmployeeNo = "E002", Name = "林美玉", Email = "mei.lin@example.com", Organization = "人資部", Title = "人資專員" },
            new EmployeeViewModel { EmployeeNo = "E003", Name = "陳建宏", Email = "jian.chen@example.com", Organization = "專案管理部", Title = "專案經理" },
            new EmployeeViewModel { EmployeeNo = "E004", Name = "趙怡君", Email = "yi.zhao@example.com", Organization = "財務部", Title = "會計" },
            new EmployeeViewModel { EmployeeNo = "E005", Name = "黃志強", Email = "zhi.huang@example.com", Organization = "研發部", Title = "前端工程師" },
            new EmployeeViewModel { EmployeeNo = "E006", Name = "李佩芬", Email = "pei.li@example.com", Organization = "研發部", Title = "後端工程師" },
            new EmployeeViewModel { EmployeeNo = "E007", Name = "周俊豪", Email = "jun.chou@example.com", Organization = "專案管理部", Title = "PMO 專員" }
        };

        [HttpGet("/employees")]
        public IActionResult Index(string? q)
        {
            var keyword = q?.Trim();
            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? Employees
                : Employees.Where(e =>
                    e.EmployeeNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    e.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    e.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            var model = new EmployeeListViewModel
            {
                Employees = filtered,
                TotalEmployees = Employees.Count,
                DepartmentCount = Employees.Select(e => e.Organization).Distinct().Count(),
                SearchResultCount = filtered.Count,
                Query = keyword
            };

            return View(model);
        }
    }
}
