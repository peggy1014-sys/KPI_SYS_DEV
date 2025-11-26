using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KpiSys.Web.Models;
using KpiSys.Web.Services;
using KpiSys.Web.Services.Kpi;

namespace KpiSys.Web.Services.Kpi;

/// <summary>
/// Implements the KPI formulas defined for phase 2.
/// </summary>
public class KpiCalculationService : IKpiCalculationService
{
    private readonly IKpiDataStore _dataStore;
    private readonly ITimesheetService _timesheetService;
    private readonly IProjectService _projectService;

    public KpiCalculationService(
        IKpiDataStore dataStore,
        ITimesheetService timesheetService,
        IProjectService projectService)
    {
        _dataStore = dataStore;
        _timesheetService = timesheetService;
        _projectService = projectService;
    }

    public Task RecalculateMonthlyAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var (from, to) = GetDateRange(year, month);
        var approvedEntries = _timesheetService.GetApprovedByRange(from, to);

        // Only process employees and projects with approved timesheets.
        if (!approvedEntries.Any())
        {
            _dataStore.UpsertScores(Array.Empty<KpiScore>());
            return Task.CompletedTask;
        }

        var workingDays = CalculateWorkingDays(from, to);
        var projectLookup = _projectService.GetAll().ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var projectTotals = approvedEntries
            .GroupBy(t => t.ProjectCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Hours + t.OvertimeHours), StringComparer.OrdinalIgnoreCase);
        var scoreDate = to;
        var scores = new List<KpiScore>();

        // Project-level KPIs: SPI, CPI, Health
        foreach (var projectGroup in approvedEntries.GroupBy(t => t.ProjectCode, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!projectLookup.TryGetValue(projectGroup.Key, out var project))
            {
                continue;
            }

            var projectTotalHours = projectTotals.TryGetValue(projectGroup.Key, out var total) ? total : 0m;
            var projectStart = project.StartDate ?? from;
            var projectEnd = project.EndDate ?? to;
            if (projectEnd < projectStart)
            {
                projectEnd = projectStart;
            }

            var totalProjectDays = Math.Max((decimal)((projectEnd - projectStart).TotalDays + 1), 1m);
            var elapsedDaysRaw = (decimal)((DateTime.Compare(projectEnd, to) > 0 ? to : projectEnd) - projectStart).TotalDays + 1m;
            var elapsedDays = Math.Max(elapsedDaysRaw, 0m);
            var progressRatio = Math.Clamp(elapsedDays / totalProjectDays, 0m, 1m);
            var plannedHoursToDate = project.BudgetHours * progressRatio;

            var spiRatio = projectTotalHours / Math.Max(plannedHoursToDate, 1m);
            var spiScore = ToScore(spiRatio);

            var hourlyCost = (project.BudgetHours > 0 && project.BudgetCost > 0)
                ? project.BudgetCost / project.BudgetHours
                : 1m;
            var ev = project.BudgetCost * progressRatio;
            var ac = projectTotalHours * hourlyCost;
            var cpiRatio = ev / Math.Max(ac, 1m);
            var cpiScore = ToScore(cpiRatio);

            // TODO: refine KPI formula with business owner
            var healthScore = Math.Round((spiScore + cpiScore) / 2m, 2);

            foreach (var employeeId in projectGroup.Select(t => t.EmployeeId).Distinct())
            {
                scores.Add(CreateScore(employeeId, project.Code, "SPI", spiScore, scoreDate));
                scores.Add(CreateScore(employeeId, project.Code, "CPI", cpiScore, scoreDate));
                scores.Add(CreateScore(employeeId, project.Code, "Health", healthScore, scoreDate));
            }
        }

        // Employee-level KPIs: OutputPerHour, Contribution, Collaboration
        var employeeGroups = approvedEntries.GroupBy(t => t.EmployeeId);
        foreach (var employeeGroup in employeeGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var employeeId = employeeGroup.Key;
            var employeeTotalHours = employeeGroup.Sum(t => t.Hours + t.OvertimeHours);
            var expectedHours = workingDays * 8m;
            var outputRatio = employeeTotalHours / Math.Max(expectedHours, 1m);
            var outputScore = ToScore(outputRatio);
            scores.Add(CreateScore(employeeId, null, "OutputPerHour", outputScore, scoreDate));

            var shares = new List<decimal>();
            foreach (var projectGroup in employeeGroup.GroupBy(t => t.ProjectCode, StringComparer.OrdinalIgnoreCase))
            {
                var empProjectHours = projectGroup.Sum(t => t.Hours + t.OvertimeHours);
                var totalProjectHours = projectTotals.TryGetValue(projectGroup.Key, out var totalHours) ? totalHours : 0m;
                var share = totalProjectHours > 0 ? empProjectHours / Math.Max(totalProjectHours, 1m) : 0m;
                shares.Add(share);
            }

            var avgShare = shares.Any() ? shares.Average() : 0m;
            var contributionScore = ToScore(avgShare * 2m); // TODO: refine KPI formula with business owner
            scores.Add(CreateScore(employeeId, null, "Contribution", contributionScore, scoreDate));

            var projectCount = employeeGroup.Select(t => t.ProjectCode).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var ratio = projectCount <= 1 ? 0.3m : projectCount >= 4 ? 1.0m : 0.3m + (projectCount - 1) * 0.2m;
            var collaborationScore = ToScore(ratio);
            scores.Add(CreateScore(employeeId, null, "Collaboration", collaborationScore, scoreDate));
        }

        _dataStore.UpsertScores(scores);
        return Task.CompletedTask;
    }

    private static (DateTime from, DateTime to) GetDateRange(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return (from, to);
    }

    private static int CalculateWorkingDays(DateTime from, DateTime to)
    {
        var workingDays = 0;
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

    private static decimal ToScore(decimal ratio)
    {
        if (ratio <= 0)
        {
            return 0m;
        }

        if (ratio >= 1)
        {
            return 100m;
        }

        return Math.Round(ratio * 100m, 2);
    }

    private static KpiScore CreateScore(int empId, string? projectCode, string kpiCode, decimal score, DateTime scoreDate)
    {
        return new KpiScore
        {
            EmpId = empId,
            ProjectCode = projectCode,
            KpiCode = kpiCode,
            Score = score,
            ScoreDate = scoreDate
        };
    }
}
