namespace KpiSys.Web.Models
{
    public class EmployeeViewModel
    {
        public string EmployeeNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class EmployeeListViewModel
    {
        public IReadOnlyList<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();
        public int TotalEmployees { get; set; }
        public int DepartmentCount { get; set; }
        public int SearchResultCount { get; set; }
        public string? Query { get; set; }
    }
}
