using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KpiSys.Web.Models;
using Microsoft.Extensions.Logging;

namespace KpiSys.Web.Services.Import;

public class EmployeeOrgImportResult
{
    public int OrganizationsRead { get; set; }
    public int OrganizationsCreated { get; set; }
    public int OrganizationsUpdated { get; set; }
    public int OrganizationsSkipped { get; set; }

    public int EmployeesRead { get; set; }
    public int EmployeesCreated { get; set; }
    public int EmployeesUpdated { get; set; }
    public int EmployeesSkipped { get; set; }
    public int EmployeesWithoutRoles { get; set; }

    public int RolesCreated { get; set; }
    public int RolesSkipped { get; set; }
    public int RolesInvalid { get; set; }
}

public interface IEmployeeOrganizationImportService
{
    Task<EmployeeOrgImportResult> ImportAsync(string organizationFilePath, string employeeFilePath, CancellationToken cancellationToken = default);
}

public class EmployeeOrganizationImportService : IEmployeeOrganizationImportService
{
    private static readonly HashSet<string> ValidRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PM", "SA", "SD", "PG", "DBA", "Operation"
    };

    private readonly IOrganizationService _organizationService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeOrganizationImportService> _logger;

    public EmployeeOrganizationImportService(
        IOrganizationService organizationService,
        IEmployeeService employeeService,
        ILogger<EmployeeOrganizationImportService> logger)
    {
        _organizationService = organizationService;
        _employeeService = employeeService;
        _logger = logger;
    }

    public async Task<EmployeeOrgImportResult> ImportAsync(string organizationFilePath, string employeeFilePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting import. Org file: {OrgFile}, Employee file: {EmployeeFile}", organizationFilePath, employeeFilePath);

        if (!File.Exists(organizationFilePath))
        {
            _logger.LogError("Organization import aborted because file was not found at path: {Path}", organizationFilePath);
            throw new FileNotFoundException("Organization import file not found.", organizationFilePath);
        }

        if (!File.Exists(employeeFilePath))
        {
            _logger.LogError("Employee import aborted because file was not found at path: {Path}", employeeFilePath);
            throw new FileNotFoundException("Employee import file not found.", employeeFilePath);
        }

        try
        {
            var orgResult = ImportOrganizations(organizationFilePath, cancellationToken);
            var employeeResult = ImportEmployeesAndRoles(employeeFilePath, orgResult.DeptCodeToOrgId);

            var totalResult = new EmployeeOrgImportResult
            {
                OrganizationsRead = orgResult.Read,
                OrganizationsCreated = orgResult.Created,
                OrganizationsUpdated = orgResult.Updated,
                OrganizationsSkipped = orgResult.Skipped,
                EmployeesRead = employeeResult.Read,
                EmployeesCreated = employeeResult.Created,
                EmployeesUpdated = employeeResult.Updated,
                EmployeesSkipped = employeeResult.Skipped,
                EmployeesWithoutRoles = employeeResult.EmptyRoles,
                RolesCreated = employeeResult.RolesCreated,
                RolesSkipped = employeeResult.RolesSkipped,
                RolesInvalid = employeeResult.RolesInvalid
            };

            _logger.LogInformation(
                "Import finished. Organizations read: {OrgRead}, created: {OrgCreated}, updated: {OrgUpdated}, skipped: {OrgSkipped}. Employees read: {EmpRead}, created: {EmpCreated}, updated: {EmpUpdated}, skipped: {EmpSkipped}, employees without roles: {EmpNoRoles}. Roles created: {RolesCreated}, skipped: {RolesSkipped}, invalid: {RolesInvalid}.",
                totalResult.OrganizationsRead,
                totalResult.OrganizationsCreated,
                totalResult.OrganizationsUpdated,
                totalResult.OrganizationsSkipped,
                totalResult.EmployeesRead,
                totalResult.EmployeesCreated,
                totalResult.EmployeesUpdated,
                totalResult.EmployeesSkipped,
                totalResult.EmployeesWithoutRoles,
                totalResult.RolesCreated,
                totalResult.RolesSkipped,
                totalResult.RolesInvalid);

            await Task.CompletedTask;
            return totalResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee and organization import failed.");
            throw;
        }
    }

    private OrganizationImportResult ImportOrganizations(string organizationFilePath, CancellationToken cancellationToken)
    {
        var result = new OrganizationImportResult();
        using var workbook = new XLWorkbook(organizationFilePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);
        _logger.LogInformation("Organization headers: {Headers}", string.Join(", ", headerMap.Keys));

        var deptCodeHeader = GetFirstExistingHeader(headerMap, "DEPT_ID", "OrgId", "OrgCode", "組織代碼", "部門代碼", "部門代號");
        var deptNameHeader = GetFirstExistingHeader(headerMap, "DEPT_NAME", "OrgName", "組織名稱", "部門名稱");
        var parentHeader = GetFirstExistingHeader(headerMap, "PARENT_DEPT_ID", "ParentOrgCode", "上層組織代碼", "上層部門代碼");
        var levelHeader = GetFirstExistingHeader(headerMap, "LEVEL", "OrgLevel", "層級");

        if (deptCodeHeader == null || deptNameHeader == null)
        {
            _logger.LogError("Organization import failed because required columns are missing. Found headers: {Headers}", string.Join(", ", headerMap.Keys));
            throw new InvalidOperationException("Organization code or name column not found in header row.");
        }

        var dataRows = worksheet.RowsUsed().Skip(1).ToList();
        result.Read = dataRows.Count;

        var records = new List<OrganizationRecord>();
        foreach (var row in dataRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deptCode = GetCellValue(row, headerMap, deptCodeHeader);
            var deptName = GetCellValue(row, headerMap, deptNameHeader);
            var parentCode = GetCellValue(row, headerMap, parentHeader);
            var levelValue = GetCellIntValue(row, headerMap, levelHeader);

            if (string.IsNullOrWhiteSpace(deptCode))
            {
                _logger.LogWarning("Skipped organization row because DEPT_ID is empty. Row number: {Row}", row.RowNumber());
                result.Skipped++;
                continue;
            }

            records.Add(new OrganizationRecord
            {
                Code = deptCode.Trim(),
                Name = string.IsNullOrWhiteSpace(deptName) ? deptCode.Trim() : deptName.Trim(),
                ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim(),
                Level = levelValue
            });
        }

        var deptCodeToOrgId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in _organizationService.GetAll().Where(o => !string.IsNullOrWhiteSpace(o.OrgCode)))
        {
            deptCodeToOrgId[existing.OrgCode!] = existing.OrgId;
        }

        foreach (var record in records)
        {
            if (!deptCodeToOrgId.ContainsKey(record.Code))
            {
                deptCodeToOrgId[record.Code] = Guid.NewGuid().ToString();
            }
        }

        var pending = new List<OrganizationRecord>(records);
        var safetyCounter = 0;
        while (pending.Count > 0 && safetyCounter < pending.Count + 5)
        {
            safetyCounter++;
            var processedThisRound = new List<OrganizationRecord>();

            foreach (var record in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                deptCodeToOrgId.TryGetValue(record.Code, out var orgId);
                string? parentOrgId = null;
                if (!string.IsNullOrWhiteSpace(record.ParentCode))
                {
                    parentOrgId = ResolveParentOrgId(record.ParentCode, deptCodeToOrgId);
                    if (string.IsNullOrWhiteSpace(parentOrgId))
                    {
                        _logger.LogWarning(
                            "Parent organization code {ParentCode} not found for {OrgCode}; importing without parent.",
                            record.ParentCode,
                            record.Code);
                    }
                    else if (!_organizationService.Exists(parentOrgId))
                    {
                        continue;
                    }
                }

                var organization = new Organization
                {
                    OrgId = orgId!,
                    OrgCode = record.Code,
                    OrgName = record.Name,
                    ParentOrgId = parentOrgId,
                    OrgLevel = record.Level ?? 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                var existing = _organizationService.GetAll()
                    .FirstOrDefault(o => string.Equals(o.OrgCode, record.Code, StringComparison.OrdinalIgnoreCase))
                    ?? _organizationService.GetById(organization.OrgId);

                var serviceResult = existing == null
                    ? _organizationService.Add(organization)
                    : _organizationService.Update(existing.OrgId, organization);

                if (serviceResult.success)
                {
                    processedThisRound.Add(record);
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
                    _logger.LogWarning("Skipped organization {OrgCode}: {Error}", record.Code, serviceResult.error);
                    result.Skipped++;
                }
            }

            foreach (var processed in processedThisRound)
            {
                pending.Remove(processed);
            }

            if (processedThisRound.Count == 0)
            {
                break;
            }
        }

        foreach (var leftover in pending)
        {
            result.Skipped++;
            _logger.LogWarning("Could not import organization {OrgCode} due to missing parent or validation issues.", leftover.Code);
        }

        result.DeptCodeToOrgId = deptCodeToOrgId;

        _logger.LogInformation(
            "Organization import summary: scanned {Scanned}, inserted {Inserted}, updated {Updated}, skipped {Skipped}.",
            result.Read,
            result.Created,
            result.Updated,
            result.Skipped);

        return result;
    }

    private EmployeeImportResult ImportEmployeesAndRoles(string employeeFilePath, Dictionary<string, string> deptCodeToOrgId)
    {
        var result = new EmployeeImportResult();

        using var workbook = new XLWorkbook(employeeFilePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);
        _logger.LogInformation("Employee headers: {Headers}", string.Join(", ", headerMap.Keys));

        var employeeNoHeader = GetFirstExistingHeader(headerMap, "empId", "EmpId", "員工編號", "EmployeeNo", "EmployeeId");
        var nameHeader = GetFirstExistingHeader(headerMap, "name", "Name", "姓名");
        var emailHeader = GetFirstExistingHeader(headerMap, "email", "Email", "帳號", "電子郵件");
        var titleHeader = GetFirstExistingHeader(headerMap, "position", "Position", "Title", "職稱");
        var statusHeader = GetFirstExistingHeader(headerMap, "status", "Status");
        var rolesHeader = GetFirstExistingHeader(headerMap, "roles", "Roles");
        var deptHeader = GetFirstExistingHeader(headerMap, "dep_ID", "DEPT_ID", "DeptId", "departmentId");

        if (employeeNoHeader == null)
        {
            _logger.LogError("Employee import failed: required employee id column is missing.");
            throw new InvalidOperationException("Employee id column not found in header row.");
        }

        if (deptHeader == null)
        {
            _logger.LogError("Employee import failed: department column not found. Headers: {Headers}", string.Join(", ", headerMap.Keys));
            throw new InvalidOperationException("Employee department column not found in header row.");
        }

        var dataRows = worksheet.RowsUsed().Skip(1).ToList();
        result.Read = dataRows.Count;

        foreach (var row in dataRows)
        {
            var employeeNo = GetCellValue(row, headerMap, employeeNoHeader);
            var name = GetCellValue(row, headerMap, nameHeader);
            var email = GetCellValue(row, headerMap, emailHeader);
            var deptCode = GetCellValue(row, headerMap, deptHeader);
            var title = GetCellValue(row, headerMap, titleHeader);
            var status = GetCellValue(row, headerMap, statusHeader);
            var rolesRaw = GetCellValue(row, headerMap, rolesHeader);

            if (string.IsNullOrWhiteSpace(employeeNo) || string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Employee row skipped because employeeNo or name is missing. Row: {Row}", row.RowNumber());
                result.Skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(deptCode) || !deptCodeToOrgId.TryGetValue(deptCode.Trim(), out var orgId))
            {
                _logger.LogWarning("Employee {EmployeeNo} ({Name}) skipped because department code '{Dept}' is missing or not found in organizations.", employeeNo, name, deptCode);
                result.Skipped++;
                continue;
            }

            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid().ToString(),
                EmployeeNo = employeeNo.Trim(),
                Name = name.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                OrgId = orgId,
                Title = string.IsNullOrWhiteSpace(title) ? "未指定" : title.Trim(),
                Status = normalizedStatus,
                SupervisorId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
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
                    result.Skipped++;
                    continue;
                }
                result.Created++;
            }
            else
            {
                var updatePayload = new Employee
                {
                    EmployeeId = existing.EmployeeId,
                    EmployeeNo = employee.EmployeeNo,
                    Name = employee.Name,
                    Email = employee.Email,
                    OrgId = employee.OrgId,
                    Title = employee.Title,
                    Status = employee.Status,
                    SupervisorId = existing.SupervisorId,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                var updateResult = _employeeService.Update(existing.Id, updatePayload);
                if (!updateResult.success)
                {
                    _logger.LogWarning("Failed to update employee {EmployeeNo}: {Error}", employee.EmployeeNo, updateResult.error);
                    result.Skipped++;
                    continue;
                }
                result.Updated++;
            }

            var target = _employeeService
                .GetAll()
                .FirstOrDefault(e => e.EmployeeNo.Equals(employee.EmployeeNo, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                result.Skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(rolesRaw))
            {
                result.EmptyRoles++;
                _logger.LogInformation("Employee {EmployeeNo} has no role assignments in source data.", employee.EmployeeNo);
                continue;
            }

            ProcessRoles(target.Id, employee.EmployeeNo, employee.Name, rolesRaw, result);
        }

        _logger.LogInformation(
            "Employee import summary: scanned {Scanned}, inserted {Inserted}, updated {Updated}, skipped {Skipped}, employees with empty roles: {EmptyRoles}.",
            result.Read,
            result.Created,
            result.Updated,
            result.Skipped,
            result.EmptyRoles);

        _logger.LogInformation(
            "Employee roles import summary: created {Created}, skipped {Skipped}, invalid {Invalid}.",
            result.RolesCreated,
            result.RolesSkipped,
            result.RolesInvalid);

        return result;
    }

    private void ProcessRoles(int employeeId, string employeeNo, string employeeName, string rolesRaw, EmployeeImportResult result)
    {
        var roleTokens = rolesRaw
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        if (roleTokens.Count == 0)
        {
            result.EmptyRoles++;
            return;
        }

        var existingRoles = _employeeService.GetRoles(employeeId).ToList();
        int? primaryRoleId = null;
        var validRoleIndex = 0;

        foreach (var token in roleTokens)
        {
            var normalized = token.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (!ValidRoleCodes.Contains(normalized))
            {
                _logger.LogWarning("Invalid role '{Role}' for employee {EmployeeNo} ({EmployeeName}). Skipping role.", token, employeeNo, employeeName);
                result.RolesInvalid++;
                continue;
            }

            var isPrimary = validRoleIndex == 0;
            validRoleIndex++;

            var existing = existingRoles.FirstOrDefault(r =>
                r.RoleCode.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                _logger.LogInformation("Employee {EmployeeNo} already has role {Role}, skipping.", employeeNo, normalized);
                result.RolesSkipped++;
                if (isPrimary)
                {
                    primaryRoleId = existing.Id;
                }
                continue;
            }

            var addResult = _employeeService.AddRole(employeeId, new EmployeeRole
            {
                RoleCode = normalized,
                RoleName = normalized,
                IsPrimary = isPrimary,
                CreatedAt = DateTime.UtcNow
            });

            if (addResult.success)
            {
                var added = _employeeService.GetRoles(employeeId)
                    .FirstOrDefault(r => r.RoleCode.Equals(normalized, StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));

                if (added != null)
                {
                    existingRoles.Add(added);
                    if (isPrimary)
                    {
                        primaryRoleId = added.Id;
                    }
                }

                result.RolesCreated++;
            }
            else
            {
                _logger.LogWarning("Failed to add role {Role} for employee {EmployeeNo}: {Error}", normalized, employeeNo, addResult.error);
                result.RolesSkipped++;
            }
        }

        if (primaryRoleId.HasValue)
        {
            _employeeService.SetPrimaryRole(employeeId, primaryRoleId.Value);
        }
    }

    private static string? ResolveParentOrgId(string? parentCode, Dictionary<string, string> deptCodeToOrgId)
    {
        if (string.IsNullOrWhiteSpace(parentCode))
        {
            return null;
        }

        if (deptCodeToOrgId.TryGetValue(parentCode.Trim(), out var parentId))
        {
            return parentId;
        }

        return null;
    }

    private class OrganizationImportResult
    {
        public int Read { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public Dictionary<string, string> DeptCodeToOrgId { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private class EmployeeImportResult
    {
        public int Read { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public int RolesCreated { get; set; }
        public int RolesSkipped { get; set; }
        public int RolesInvalid { get; set; }
        public int Skipped { get; set; }
        public int EmptyRoles { get; set; }
    }

    private class OrganizationRecord
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public int? Level { get; set; }
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
    {
        return headerRow.Cells().ToDictionary(
            cell => cell.GetString().Trim(),
            cell => cell.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetFirstExistingHeader(Dictionary<string, int> headerMap, params string[] keys)
    {
        return keys.FirstOrDefault(headerMap.ContainsKey);
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> headerMap, string? headerKey)
    {
        if (!string.IsNullOrWhiteSpace(headerKey) && headerMap.TryGetValue(headerKey, out var columnNumber))
        {
            return row.Cell(columnNumber).GetString().Trim();
        }

        return string.Empty;
    }

    private static int? GetCellIntValue(IXLRow row, Dictionary<string, int> headerMap, string? headerKey)
    {
        var value = GetCellValue(row, headerMap, headerKey);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}
