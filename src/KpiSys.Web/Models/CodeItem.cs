namespace KpiSys.Web.Models;

public class CodeItem
{
    public string CodeSet { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string CodeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}
