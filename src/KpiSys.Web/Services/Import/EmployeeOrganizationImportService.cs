using ClosedXML.Excel;
using KpiSys.Web.Models;
using Microsoft.Extensions.Logging;

namespace KpiSys.Web.Services.Import;

public class EmployeeOrgImportResult
{
    public int OrganizationsRead { get; set; }

    public int OrganizationsCreated { get; set; }

    public int OrganizationsUpdated { get; set; }

    public int EmployeesRead { get; set; }

    public int EmployeesCreated { get; set; }

    public int EmployeesUpdated { get; set; }

    public int RolesLinked { get; set; }
}

public interface IEmployeeOrganizationImportService
{
    Task<EmployeeOrgImportResult> ImportAsync(string organizationFilePath, string employeeFilePath, CancellationToken cancellationToken = default);
}

public class EmployeeOrganizationImportService : IEmployeeOrganizationImportService
{
    private const string DefaultOrgId = "UNASSIGNED";
    private const string DefaultOrgName = "未分類部門";
    private const string EmployeeRoleCodeSet = "EMP_ROLE";

    private readonly IOrganizationService _organizationService;
    private readonly IEmployeeService _employeeService;
    private readonly ICodeService _codeService;
    private readonly ILogger<EmployeeOrganizationImportService> _logger;

    public EmployeeOrganizationImportService(
        IOrganizationService organizationService,
        IEmployeeService employeeService,
        ICodeService codeService,
        ILogger<EmployeeOrganizationImportService> logger)
    {
        _organizationService = organizationService;
        _employeeService = employeeService;
        _codeService = codeService;
        _logger = logger;
    }

    public async Task<EmployeeOrgImportResult> ImportAsync(string organizationFilePath, string employeeFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting import. Org file: {OrgFile}, Employee file: {EmployeeFile}", organizationFilePath, employeeFilePath);

        var orgResult = ImportOrganizations(organizationFilePath, cancellationToken);
        EnsureDefaultOrganizationExists();
        var employeeResult = ImportEmployeesAndRoles(employeeFilePath);

        var totalResult = new EmployeeOrgImportResult
        {
            OrganizationsRead = orgResult.Read,
            OrganizationsCreated = orgResult.Created,
            OrganizationsUpdated = orgResult.Updated,
            EmployeesRead = employeeResult.Read,
            EmployeesCreated = employeeResult.Created,
            EmployeesUpdated = employeeResult.Updated,
            RolesLinked = employeeResult.RolesLinked
        };

        _logger.LogInformation(
            "Import finished. Organizations read: {OrgRead}, created: {OrgCreated}, updated: {OrgUpdated}. Employees read: {EmpRead}, created: {EmpCreated}, updated: {EmpUpdated}. Roles linked: {RolesLinked}.",
            totalResult.OrganizationsRead,
            totalResult.OrganizationsCreated,
            totalResult.OrganizationsUpdated,
            totalResult.EmployeesRead,
            totalResult.EmployeesCreated,
            totalResult.EmployeesUpdated,
            totalResult.RolesLinked);

        await Task.CompletedTask;
        return totalResult;
    }

    private OrganizationImportResult ImportOrganizations(string organizationFilePath, CancellationToken cancellationToken)
    {
        var result = new OrganizationImportResult();

        if (!File.Exists(organizationFilePath))
        {
            _logger.LogWarning("Organization file not found: {Path}", organizationFilePath);
            return result;
        }

        using var workbook = new XLWorkbook(organizationFilePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);

        var records = worksheet
            .RowsUsed()
            .Skip(1)
            .Select(row => new OrganizationRecord
            {
                Code = GetCellValue(row, headerMap, "OrgCode", "組織代碼", "部門代碼", "OrgId", "部門代號"),
                Name = GetCellValue(row, headerMap, "OrgName", "組織名稱", "部門名稱"),
                ParentCode = GetCellValue(row, headerMap, "ParentOrgCode", "上層組織代碼", "ParentOrgId", "上層部門代碼"),
                Level = GetCellIntValue(row, headerMap, "OrgLevel", "Level", "層級")
            })
            .Where(record => !string.IsNullOrWhiteSpace(record.Code))
            .ToList();

        result.Read = records.Count;

        var pending = new List<OrganizationRecord>(records);
        var safetyCounter = 0;
        while (pending.Count > 0 && safetyCounter < pending.Count + 5)
        {
            safetyCounter++;
            var processedInRound = new List<OrganizationRecord>();

            foreach (var record in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parentId = string.IsNullOrWhiteSpace(record.ParentCode) ? null : record.ParentCode.Trim();
                if (!string.IsNullOrWhiteSpace(parentId) && _organizationService.GetById(parentId) == null)
                {
                    continue;
                }

                var organization = new Organization
                {
                    OrgId = record.Code.Trim(),
                    OrgName = string.IsNullOrWhiteSpace(record.Name) ? record.Code.Trim() : record.Name.Trim(),
                    ParentOrgId = parentId,
                    OrgLevel = record.Level ?? 0
                };

                var existing = _organizationService.GetById(organization.OrgId);
                var serviceResult = existing == null
                    ? _organizationService.Add(organization)
                    : _organizationService.Update(organization.OrgId, organization);

                if (serviceResult.success)
                {
                    processedInRound.Add(record);
                    if (existing == null)
                    {
                        result.Created++;
                    }
                    else
                    {
                        result.Updated++;
                    }
                }
                else
                {
                    _logger.LogWarning("Skipped organization {OrgId}: {Error}", organization.OrgId, serviceResult.error);
                }
            }

            foreach (var processed in processedInRound)
            {
                pending.Remove(processed);
            }

            if (processedInRound.Count == 0)
            {
                break;
            }
        }

        foreach (var leftover in pending)
        {
            _logger.LogWarning("Could not import organization {OrgId} due to missing parent or validation issues.", leftover.Code);
        }

        return result;
    }

    private EmployeeImportResult ImportEmployeesAndRoles(string employeeFilePath)
    {
        var result = new EmployeeImportResult();

        if (!File.Exists(employeeFilePath))
        {
            _logger.LogWarning("Employee file not found: {Path}", employeeFilePath);
            return result;
        }

        using var workbook = new XLWorkbook(employeeFilePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var employeeNo = GetCellValue(row, headerMap, "EmpId", "員工編號", "EmployeeNo", "EmployeeId");
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                continue;
            }

            result.Read++;

            var name = GetCellValue(row, headerMap, "Name", "姓名");
            var email = GetCellValue(row, headerMap, "Email", "帳號", "電子郵件");
            var orgId = GetCellValue(row, headerMap, "DeptCode", "部門代碼", "OrgCode", "OrgId", "部門");
            var title = GetCellValue(row, headerMap, "Title", "職稱");
            var rolesRaw = GetCellValue(row, headerMap, "職掌", "Roles", "Role", "RoleName");

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Employee {EmployeeNo} skipped because name is empty.", employeeNo);
                continue;
            }

            if (string.IsNullOrWhiteSpace(orgId) || _organizationService.GetById(orgId.Trim()) == null)
            {
                _logger.LogWarning("Employee {EmployeeNo} assigned to fallback organization because org {Org} not found.", employeeNo, orgId);
                orgId = DefaultOrgId;
            }

            var employee = new Employee
            {
                EmployeeNo = employeeNo.Trim(),
                Name = name.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                OrgId = orgId.Trim(),
                Title = string.IsNullOrWhiteSpace(title) ? "未指定" : title.Trim()
            };

            var existing = _employeeService
                .GetAll()
                .FirstOrDefault(e => e.EmployeeNo.Equals(employee.EmployeeNo, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                var createResult = _employeeService.Create(employee);
                if (!createResult.success)
                {
                    _logger.LogWarning("Failed to create employee {EmployeeNo}: {Error}", employee.EmployeeNo, createResult.error);
                    continue;
                }
                result.Created++;
            }
            else
            {
                var updateResult = _employeeService.Update(existing.Id, employee);
                if (!updateResult.success)
                {
                    _logger.LogWarning("Failed to update employee {EmployeeNo}: {Error}", employee.EmployeeNo, updateResult.error);
                    continue;
                }
                result.Updated++;
            }

            if (!string.IsNullOrWhiteSpace(rolesRaw))
            {
                var target = _employeeService
                    .GetAll()
                    .FirstOrDefault(e => e.EmployeeNo.Equals(employee.EmployeeNo, StringComparison.OrdinalIgnoreCase));

                if (target != null)
                {
                    result.RolesLinked += AssignRoles(target.Id, rolesRaw);
                }
            }
        }

        return result;
    }

    private int AssignRoles(int employeeId, string rolesRaw)
    {
        var roles = rolesRaw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        if (roles.Count == 0)
        {
            return 0;
        }

        EnsureRoleCodes(roles);

        int? desiredPrimaryRoleId = null;
        var newLinks = 0;
        var existingRoles = _employeeService.GetRoles(employeeId).ToList();
        for (var i = 0; i < roles.Count; i++)
        {
            var role = roles[i];
            var isPrimary = i == 0;
            var existing = existingRoles.FirstOrDefault(r => r.RoleName.Equals(role, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (isPrimary)
                {
                    desiredPrimaryRoleId = existing.Id;
                }

                continue;
            }

            var addResult = _employeeService.AddRole(employeeId, new EmployeeRole
            {
                RoleName = role,
                IsPrimary = isPrimary
            });

            if (addResult.success)
            {
                newLinks++;
                var addedRole = _employeeService
                    .GetRoles(employeeId)
                    .FirstOrDefault(r => r.RoleName.Equals(role, StringComparison.OrdinalIgnoreCase));

                if (addedRole != null)
                {
                    existingRoles.Add(addedRole);

                    if (isPrimary)
                    {
                        desiredPrimaryRoleId = addedRole.Id;
                    }
                }
            }

            if (!addResult.success)
            {
                _logger.LogWarning("Failed to add role {Role} for employee {EmployeeId}: {Error}", role, employeeId, addResult.error);
            }
        }

        if (desiredPrimaryRoleId.HasValue)
        {
            _employeeService.SetPrimaryRole(employeeId, desiredPrimaryRoleId.Value);
        }

        return newLinks;
    }

    private void EnsureRoleCodes(IEnumerable<string> roles)
    {
        var existingCodes = _codeService.GetCodes(EmployeeRoleCodeSet);
        var sortOrder = existingCodes.Count + 1;

        foreach (var role in roles)
        {
            var hasCode = existingCodes.Any(c => c.Code.Equals(role, StringComparison.OrdinalIgnoreCase));
            if (hasCode)
            {
                continue;
            }

            var result = _codeService.AddCode(new CodeItem
            {
                CodeSet = EmployeeRoleCodeSet,
                Code = role,
                CodeName = role,
                SortOrder = sortOrder++
            });

            if (!result.success)
            {
                _logger.LogWarning("Failed to add code {Role} to set {CodeSet}: {Error}", role, EmployeeRoleCodeSet, result.error);
            }
        }
    }

    private void EnsureDefaultOrganizationExists()
    {
        if (_organizationService.GetById(DefaultOrgId) != null)
        {
            return;
        }

        var result = _organizationService.Add(new Organization
        {
            OrgId = DefaultOrgId,
            OrgName = DefaultOrgName,
            OrgLevel = 1
        });

        if (!result.success)
        {
            _logger.LogWarning("Failed to create default organization {OrgId}: {Error}", DefaultOrgId, result.error);
        }
    }

    private class OrganizationImportResult
    {
        public int Read { get; set; }

        public int Created { get; set; }

        public int Updated { get; set; }
    }

    private class EmployeeImportResult
    {
        public int Read { get; set; }

        public int Created { get; set; }

        public int Updated { get; set; }

        public int RolesLinked { get; set; }
    }

    private class OrganizationRecord
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string ParentCode { get; set; } = string.Empty;

        public int? Level { get; set; }
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
    {
        return headerRow.Cells().ToDictionary(
            cell => cell.GetString().Trim(),
            cell => cell.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> headerMap, params string[] headerKeys)
    {
        foreach (var key in headerKeys)
        {
            if (headerMap.TryGetValue(key, out var columnNumber))
            {
                return row.Cell(columnNumber).GetString().Trim();
            }
        }

        return string.Empty;
    }

    private static int? GetCellIntValue(IXLRow row, Dictionary<string, int> headerMap, params string[] headerKeys)
    {
        var value = GetCellValue(row, headerMap, headerKeys);
        return int.TryParse(value, out var number) ? number : null;
    }
}
