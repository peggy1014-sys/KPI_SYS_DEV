namespace KpiSys.Web.Services.Kpi;

/// <summary>
/// Defines KPI calculation entry points.
/// </summary>
public interface IKpiCalculationService
{
    /// <summary>
    /// Recalculate KPI scores for a specific month.
    /// </summary>
    Task RecalculateMonthlyAsync(int year, int month, CancellationToken cancellationToken = default);
}
