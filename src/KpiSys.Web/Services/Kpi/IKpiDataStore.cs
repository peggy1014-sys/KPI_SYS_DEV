using KpiSys.Web.Models;

namespace KpiSys.Web.Services.Kpi;

/// <summary>
/// Provides persistence for KPI masters and calculated scores.
/// </summary>
public interface IKpiDataStore
{
    IReadOnlyList<KpiMaster> GetMasters();

    IReadOnlyList<KpiScore> GetScoresByMonth(int year, int month);

    void UpsertScores(IEnumerable<KpiScore> scores);
}
