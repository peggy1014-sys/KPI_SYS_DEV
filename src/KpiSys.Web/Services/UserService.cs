using System.Collections.Concurrent;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface IUserService
{
    IReadOnlyList<UserAccount> GetAll();
    UserAccount? GetById(int id);
    (bool success, string? error) Create(UserAccount user);
    (bool success, string? error) Update(int id, UserAccount updated);
    (bool success, string? error) Delete(int id);
    UserAccount? Authenticate(string email, string password);
}

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<int, UserAccount> _users = new();
    private readonly IEmployeeService _employeeService;
    private int _userId = 1;

    public UserService(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
        Seed();
    }

    public IReadOnlyList<UserAccount> GetAll() => _users.Values.OrderBy(u => u.Id).ToList();

    public UserAccount? GetById(int id) => _users.TryGetValue(id, out var user) ? user : null;

    public UserAccount? Authenticate(string email, string password)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            return null;
        }

        return user.Password == password ? user : null;
    }

    public (bool success, string? error) Create(UserAccount user)
    {
        var validation = Validate(user, null);
        if (!validation.success)
        {
            return validation;
        }

        user.Id = Interlocked.Increment(ref _userId);
        _users.TryAdd(user.Id, Normalize(user));
        return (true, null);
    }

    public (bool success, string? error) Update(int id, UserAccount updated)
    {
        if (!_users.ContainsKey(id))
        {
            return (false, "找不到使用者");
        }

        var validation = Validate(updated, id);
        if (!validation.success)
        {
            return validation;
        }

        var existing = _users[id];
        if (string.IsNullOrWhiteSpace(updated.Password))
        {
            updated.Password = existing.Password;
        }

        updated.Id = id;
        _users[id] = Normalize(updated);
        return (true, null);
    }

    public (bool success, string? error) Delete(int id)
    {
        var removed = _users.TryRemove(id, out _);
        return removed ? (true, null) : (false, "找不到使用者");
    }

    private (bool success, string? error) Validate(UserAccount user, int? updatingId)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, "Email 必填");
        }

        if (_users.Values.Any(u => u.Email.Equals(user.Email.Trim(), StringComparison.OrdinalIgnoreCase) && u.Id != updatingId))
        {
            return (false, "Email 已存在");
        }

        if (user.EmployeeId.HasValue && _employeeService.GetById(user.EmployeeId.Value) == null)
        {
            return (false, "綁定的員工不存在");
        }

        if (string.IsNullOrWhiteSpace(user.Name))
        {
            return (false, "姓名必填");
        }

        if (string.IsNullOrWhiteSpace(user.Role))
        {
            return (false, "角色必填");
        }

        if (updatingId == null && string.IsNullOrWhiteSpace(user.Password))
        {
            return (false, "密碼必填");
        }

        return (true, null);
    }

    private static UserAccount Normalize(UserAccount user)
    {
        return new UserAccount
        {
            Id = user.Id,
            Name = user.Name.Trim(),
            Email = user.Email.Trim(),
            Role = user.Role.Trim(),
            Password = string.IsNullOrWhiteSpace(user.Password) ? string.Empty : user.Password.Trim(),
            EmployeeId = user.EmployeeId
        };
    }

    private void Seed()
    {
        var seedUsers = new List<UserAccount>
        {
            new() { Name = "系統管理員", Email = "admin@example.com", Role = "Admin", Password = "admin123", EmployeeId = _employeeService.GetAll().FirstOrDefault()?.Id },
            new() { Name = "審核主管", Email = "manager@example.com", Role = "Manager", Password = "manager123", EmployeeId = _employeeService.GetAll().FirstOrDefault()?.Id },
            new() { Name = "專案經理", Email = "pm@example.com", Role = "PM", Password = "pm12345", EmployeeId = _employeeService.GetAll().FirstOrDefault()?.Id },
            new() { Name = "一般員工", Email = "staff@example.com", Role = "Employee", Password = "staff123", EmployeeId = _employeeService.GetAll().Skip(1).FirstOrDefault()?.Id },
        };

        foreach (var user in seedUsers)
        {
            user.Id = Interlocked.Increment(ref _userId);
            _users.TryAdd(user.Id, Normalize(user));
        }
    }
}
