using KpiSys.Web;
using KpiSys.Web.Services;
using KpiSys.Web.Services.Kpi;
using KpiSys.Web.Services.Import;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IOrganizationService, OrganizationService>();
builder.Services.AddSingleton<IEmployeeService, EmployeeService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ICodeService, CodeService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<ITimesheetService, TimesheetService>();
builder.Services.AddSingleton<IKpiDataStore, KpiDataStore>();
builder.Services.AddScoped<IKpiCalculationService, KpiCalculationService>();
builder.Services.AddSingleton<IEmployeeOrganizationImportService, EmployeeOrganizationImportService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

var importRequested = args.Any(a => a.Equals("--import-employee-org", StringComparison.OrdinalIgnoreCase)
    || a.Equals("import-employee-org", StringComparison.OrdinalIgnoreCase));

if (importRequested)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.WriteLine("The import command is only available in Development environment.");
        return;
    }

    using var scope = app.Services.CreateScope();
    var importService = scope.ServiceProvider.GetRequiredService<IEmployeeOrganizationImportService>();
    var contentRoot = app.Environment.ContentRootPath;
    var orgPath = Path.Combine(contentRoot, "db", "import", "組織資料匯出_20251022.xlsx");
    var employeePath = Path.Combine(contentRoot, "db", "import", "employees_2025-11-27.xlsx");

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var result = await importService.ImportAsync(orgPath, employeePath);
            var summaryMessage =
                $"Organizations: scanned {result.OrganizationsRead}, inserted {result.OrganizationsCreated}, updated {result.OrganizationsUpdated}, skipped {result.OrganizationsSkipped}. " +
                $"Employees: scanned {result.EmployeesRead}, inserted {result.EmployeesCreated}, updated {result.EmployeesUpdated}, skipped {result.EmployeesSkipped}, employees without roles {result.EmployeesWithoutRoles}. " +
                $"EmployeeRoles: created {result.RolesCreated}, skipped {result.RolesSkipped}, invalid {result.RolesInvalid}.";

        logger.LogInformation("{Summary}", summaryMessage);
        Console.WriteLine(summaryMessage);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Import command failed.");
        Console.WriteLine("Import failed: " + ex.Message);
    }
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

namespace KpiSys.Web
{
    // Used to anchor assembly-level attributes or shared constants later.
    public partial class Program { }
}
