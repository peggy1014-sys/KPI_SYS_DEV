using System;
using System.Collections.Generic;
using System.Linq;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web.Services.Kpi;
using Microsoft.AspNetCore.Mvc;

namespace KpiSys.Web.Controllers;

/// <summary>
/// Simple admin surface to trigger KPI recalculations and review results.
/// </summary>
[SessionAuthorize("Admin")]
public class KpiAdminController : Controller
{
    private readonly IKpiCalculationService _kpiCalculationService;
    private readonly IKpiDataStore _kpiDataStore;
    private readonly IEmployeeService _employeeService;
    private readonly IProjectService _projectService;

    public KpiAdminController(
        IKpiCalculationService kpiCalculationService,
        IKpiDataStore kpiDataStore,
        IEmployeeService employeeService,
        IProjectService projectService)
    {
        _kpiCalculationService = kpiCalculationService;
        _kpiDataStore = kpiDataStore;
        _employeeService = employeeService;
        _projectService = projectService;
    }

    [HttpGet]
    public IActionResult Index(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Today.Year;
        var targetMonth = month ?? DateTime.Today.Month;

        var scores = _kpiDataStore.GetScoresByMonth(targetYear, targetMonth);
        var employees = _employeeService.GetAll().ToDictionary(e => e.Id);
        var projects = _projectService.GetAll().ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var kpiCodes = _kpiDataStore.GetMasters().Select(m => m.KpiCode).ToList();

        var items = new Dictionary<(int, string?), KpiScoreRow>();
        foreach (var score in scores)
        {
            var key = (score.EmpId, score.ProjectCode);
            if (!items.TryGetValue(key, out var row))
            {
                employees.TryGetValue(score.EmpId, out var employee);
                string? projectName = null;
                if (!string.IsNullOrWhiteSpace(score.ProjectCode) && projects.TryGetValue(score.ProjectCode, out var project))
                {
                    projectName = project.Name;
                }

                row = new KpiScoreRow
                {
                    EmployeeId = score.EmpId,
                    EmployeeName = employee?.Name ?? $"員工 {score.EmpId}",
                    ProjectCode = score.ProjectCode,
                    ProjectName = projectName,
                    ScoreDate = score.ScoreDate
                };
                items[key] = row;
            }

            row.Scores[score.KpiCode] = score.Score;
        }

        var viewModel = new KpiAdminViewModel
        {
            Year = targetYear,
            Month = targetMonth,
            KpiCodes = kpiCodes,
            Items = items.Values
                .OrderBy(v => v.EmployeeName)
                .ThenBy(v => v.ProjectCode ?? string.Empty)
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(int year, int month)
    {
        await _kpiCalculationService.RecalculateMonthlyAsync(year, month);
        return RedirectToAction(nameof(Index), new { year, month });
    }
}
