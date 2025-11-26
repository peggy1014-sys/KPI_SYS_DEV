using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services.Kpi;

/// <summary>
/// In-memory persistence for KPI masters and calculated scores.
/// </summary>
public class KpiDataStore : IKpiDataStore
{
    private readonly ConcurrentDictionary<int, KpiScore> _scores = new();
    private readonly List<KpiMaster> _masters;
    private int _scoreId;
    private readonly object _syncRoot = new();

    public KpiDataStore()
    {
        _masters = SeedMasters();
        _scoreId = _masters.Count;
    }

    public IReadOnlyList<KpiMaster> GetMasters() => _masters.Select(Clone).ToList();

    public IReadOnlyList<KpiScore> GetScoresByMonth(int year, int month)
    {
        return _scores.Values
            .Where(s => s.ScoreDate.Year == year && s.ScoreDate.Month == month)
            .OrderBy(s => s.EmpId)
            .ThenBy(s => s.ProjectCode)
            .ThenBy(s => s.KpiCode)
            .Select(Clone)
            .ToList();
    }

    public void UpsertScores(IEnumerable<KpiScore> scores)
    {
        if (scores == null)
        {
            return;
        }

        lock (_syncRoot)
        {
            foreach (var score in scores)
            {
                var existing = _scores
                    .Where(s => s.Value.EmpId == score.EmpId
                                && string.Equals(s.Value.ProjectCode ?? string.Empty, score.ProjectCode ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(s.Value.KpiCode, score.KpiCode, StringComparison.OrdinalIgnoreCase)
                                && s.Value.ScoreDate.Date == score.ScoreDate.Date)
                    .Select(s => s.Key)
                    .ToList();

                foreach (var key in existing)
                {
                    _scores.TryRemove(key, out _);
                }

                var cloned = Clone(score);
                cloned.Id = Interlocked.Increment(ref _scoreId);
                cloned.CreatedAt = DateTime.Now;
                _scores[cloned.Id] = cloned;
            }
        }
    }

    private static List<KpiMaster> SeedMasters()
    {
        var now = DateTime.Now;
        return new List<KpiMaster>
        {
            new() { Id = 1, KpiCode = "SPI", KpiName = "Schedule Performance Index", Description = "進度績效指標", CreatedAt = now },
            new() { Id = 2, KpiCode = "CPI", KpiName = "Cost Performance Index", Description = "成本績效指標", CreatedAt = now },
            new() { Id = 3, KpiCode = "Health", KpiName = "Project Health", Description = "專案健康度", CreatedAt = now },
            new() { Id = 4, KpiCode = "Contribution", KpiName = "Contribution", Description = "專案貢獻度", CreatedAt = now },
            new() { Id = 5, KpiCode = "OutputPerHour", KpiName = "Output Per Hour", Description = "工時效率", CreatedAt = now },
            new() { Id = 6, KpiCode = "Collaboration", KpiName = "Collaboration", Description = "跨專案協作", CreatedAt = now }
        };
    }

    private static KpiMaster Clone(KpiMaster master)
    {
        return new KpiMaster
        {
            Id = master.Id,
            KpiCode = master.KpiCode,
            KpiName = master.KpiName,
            Formula = master.Formula,
            Description = master.Description,
            CreatedAt = master.CreatedAt
        };
    }

    private static KpiScore Clone(KpiScore score)
    {
        return new KpiScore
        {
            Id = score.Id,
            EmpId = score.EmpId,
            ProjectCode = score.ProjectCode,
            KpiCode = score.KpiCode,
            ScoreDate = score.ScoreDate,
            Score = score.Score,
            CreatedAt = score.CreatedAt
        };
    }
}
