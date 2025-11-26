using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

/// <summary>
/// KPI master record that defines available KPI codes and their metadata.
/// </summary>
public class KpiMaster
{
    public int Id { get; set; }

    [Required]
    public string KpiCode { get; set; } = string.Empty;

    [Required]
    public string KpiName { get; set; } = string.Empty;

    public string? Formula { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// KPI score result for a given employee and project (if applicable).
/// </summary>
public class KpiScore
{
    public int Id { get; set; }

    public int EmpId { get; set; }

    public string? ProjectCode { get; set; }

    public string KpiCode { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime ScoreDate { get; set; }

    public decimal Score { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// View model for listing monthly KPI scores.
/// </summary>
public class KpiAdminViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public List<string> KpiCodes { get; set; } = new();

    public List<KpiScoreRow> Items { get; set; } = new();
}

/// <summary>
/// Aggregated KPI scores per employee and project.
/// </summary>
public class KpiScoreRow
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string? ProjectCode { get; set; }

    public string? ProjectName { get; set; }

    public DateTime ScoreDate { get; set; }

    public Dictionary<string, decimal> Scores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
