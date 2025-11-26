using System.Collections.Concurrent;
using KpiSys.Web.Models;

namespace KpiSys.Web.Services;

public interface ICodeService
{
    IReadOnlyCollection<string> GetCodeSets();

    IReadOnlyCollection<CodeItem> GetCodes(string codeSet);

    (bool success, string? error) AddCode(CodeItem item);

    (bool success, string? error) UpdateCode(string codeSet, string code, CodeItem updatedItem);

    bool DeleteCode(string codeSet, string code);
}

public class CodeService : ICodeService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CodeItem>> _codes;

    public CodeService()
    {
        _codes = new ConcurrentDictionary<string, ConcurrentDictionary<string, CodeItem>>(StringComparer.OrdinalIgnoreCase);
        SeedDefaults();
    }

    public IReadOnlyCollection<string> GetCodeSets()
    {
        return _codes.Keys.OrderBy(k => k).ToList();
    }

    public IReadOnlyCollection<CodeItem> GetCodes(string codeSet)
    {
        if (!_codes.TryGetValue(codeSet, out var items))
        {
            return Array.Empty<CodeItem>();
        }

        return items.Values
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToList();
    }

    public (bool success, string? error) AddCode(CodeItem item)
    {
        if (string.IsNullOrWhiteSpace(item.CodeSet))
        {
            return (false, "codeSet is required.");
        }

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            return (false, "code is required.");
        }

        if (string.IsNullOrWhiteSpace(item.CodeName))
        {
            return (false, "codeName is required.");
        }

        var set = _codes.GetOrAdd(item.CodeSet.Trim(), _ => new ConcurrentDictionary<string, CodeItem>(StringComparer.OrdinalIgnoreCase));
        if (!set.TryAdd(item.Code.Trim(), Normalize(item)))
        {
            return (false, "codeSet + code must be unique.");
        }

        return (true, null);
    }

    public (bool success, string? error) UpdateCode(string codeSet, string code, CodeItem updatedItem)
    {
        if (string.IsNullOrWhiteSpace(updatedItem.CodeName))
        {
            return (false, "codeName is required.");
        }

        if (!_codes.TryGetValue(codeSet, out var set))
        {
            return (false, "code not found.");
        }

        if (!set.TryGetValue(code, out var existing))
        {
            return (false, "code not found.");
        }

        var normalized = Normalize(new CodeItem
        {
            CodeSet = existing.CodeSet,
            Code = existing.Code,
            CodeName = updatedItem.CodeName,
            Description = updatedItem.Description,
            SortOrder = updatedItem.SortOrder,
        });

        set[code] = normalized;
        return (true, null);
    }

    public bool DeleteCode(string codeSet, string code)
    {
        if (!_codes.TryGetValue(codeSet, out var set))
        {
            return false;
        }

        return set.TryRemove(code, out _);
    }

    private static CodeItem Normalize(CodeItem item)
    {
        return new CodeItem
        {
            CodeSet = item.CodeSet.Trim(),
            Code = item.Code.Trim(),
            CodeName = item.CodeName.Trim(),
            Description = item.Description?.Trim(),
            SortOrder = item.SortOrder,
        };
    }

    private void SeedDefaults()
    {
        var defaults = new List<CodeItem>
        {
            new() { CodeSet = "PROJECT_SIZE", Code = "S", CodeName = "小型", Description = "預算<500萬", SortOrder = 1 },
            new() { CodeSet = "PROJECT_SIZE", Code = "M", CodeName = "中型", Description = "預算500-2000萬", SortOrder = 2 },
            new() { CodeSet = "PROJECT_SIZE", Code = "L", CodeName = "大型", Description = "預算2000-5000萬", SortOrder = 3 },
            new() { CodeSet = "PROJECT_SIZE", Code = "XL", CodeName = "超大型", Description = "預算>5000萬", SortOrder = 4 },
            new() { CodeSet = "PROJECT_CRITICALITY", Code = "一般", CodeName = "一般", Description = "影響範圍小", SortOrder = 1 },
            new() { CodeSet = "PROJECT_CRITICALITY", Code = "重要", CodeName = "重要", Description = "影響部門運作", SortOrder = 2 },
            new() { CodeSet = "PROJECT_CRITICALITY", Code = "重大", CodeName = "重大", Description = "影響公司營運", SortOrder = 3 },
            new() { CodeSet = "PROJECT_CRITICALITY", Code = "關鍵", CodeName = "關鍵", Description = "影響公司存續", SortOrder = 4 },
            new() { CodeSet = "PROJECT_STATUS", Code = "規劃中", CodeName = "規劃中", Description = "專案規劃階段", SortOrder = 1 },
            new() { CodeSet = "PROJECT_STATUS", Code = "執行中", CodeName = "執行中", Description = "專案執行階段", SortOrder = 2 },
            new() { CodeSet = "PROJECT_STATUS", Code = "暫停", CodeName = "暫停", Description = "專案暫停", SortOrder = 3 },
            new() { CodeSet = "PROJECT_STATUS", Code = "結案", CodeName = "結案", Description = "專案已結案", SortOrder = 4 },
            new() { CodeSet = "PROJECT_STATUS", Code = "取消", CodeName = "取消", Description = "專案已取消", SortOrder = 5 },
            new() { CodeSet = "PROJECT_TYPE", Code = "開發", CodeName = "開發", Description = "系統開發專案", SortOrder = 1 },
            new() { CodeSet = "PROJECT_TYPE", Code = "維運", CodeName = "維運", Description = "系統維運專案", SortOrder = 2 },
            new() { CodeSet = "PROJECT_TYPE", Code = "研究", CodeName = "研究", Description = "技術研究專案", SortOrder = 3 },
            new() { CodeSet = "PROJECT_TYPE", Code = "教育訓練", CodeName = "教育訓練", Description = "教育訓練專案", SortOrder = 4 },
            new() { CodeSet = "PORTFOLIO", Code = "FIN", CodeName = "財務組合", Description = "財務相關專案", SortOrder = 1 },
            new() { CodeSet = "PORTFOLIO", Code = "HR", CodeName = "人資組合", Description = "人資相關專案", SortOrder = 2 },
            new() { CodeSet = "PORTFOLIO", Code = "OPS", CodeName = "營運組合", Description = "營運相關專案", SortOrder = 3 },
            new() { CodeSet = "TASK_GROUP", Code = "kickoff", CodeName = "專案啟動", Description = "專案啟動會議", SortOrder = 1 },
            new() { CodeSet = "TASK_GROUP", Code = "需求訪談", CodeName = "需求訪談", Description = "使用者需求收集", SortOrder = 2 },
            new() { CodeSet = "TASK_GROUP", Code = "資源申請", CodeName = "資源申請", Description = "人力與設備申請", SortOrder = 3 },
            new() { CodeSet = "TASK_GROUP", Code = "系統分析", CodeName = "系統分析", Description = "系統分析與設計", SortOrder = 4 },
            new() { CodeSet = "TASK_GROUP", Code = "UT", CodeName = "單元測試", Description = "單元測試", SortOrder = 5 },
            new() { CodeSet = "TASK_GROUP", Code = "SIT/UAT(教育訓練)", CodeName = "整合測試", Description = "整合測試與使用者驗收", SortOrder = 6 },
            new() { CodeSet = "TASK_GROUP", Code = "上線驗收", CodeName = "上線驗收", Description = "系統上線與驗收", SortOrder = 7 },
            new() { CodeSet = "DEPEND_TYPE", Code = "FS", CodeName = "Finish-to-Start", Description = "完成後開始", SortOrder = 1 },
            new() { CodeSet = "DEPEND_TYPE", Code = "SS", CodeName = "Start-to-Start", Description = "同時開始", SortOrder = 2 },
            new() { CodeSet = "DEPEND_TYPE", Code = "FF", CodeName = "Finish-to-Finish", Description = "同時完成", SortOrder = 3 },
            new() { CodeSet = "DEPEND_TYPE", Code = "SF", CodeName = "Start-to-Finish", Description = "開始後完成", SortOrder = 4 },
        };

        foreach (var item in defaults)
        {
            AddCode(item);
        }
    }
}
