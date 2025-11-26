using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class UserAccount
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "角色")]
    public string Role { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "員工綁定")]
    public int? EmployeeId { get; set; }
}

public class UserListViewModel
{
    public List<UserAccount> Users { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public UserAccount NewUser { get; set; } = new();
}
