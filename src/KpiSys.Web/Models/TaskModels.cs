using System.ComponentModel.DataAnnotations;

namespace KpiSys.Web.Models;

public class ProjectTask
{
    public int Id { get; set; }

    [Required]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "任務名稱")]
    public string TaskName { get; set; } = string.Empty;

    [Display(Name = "任務群組")]
    public string? TaskGroup { get; set; }

    [Display(Name = "負責人")]
    public int? ResponsibleId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "計畫開始日")]
    public DateTime? PlanStart { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "計畫結束日")]
    public DateTime? PlanEnd { get; set; }

    [Display(Name = "進度%")]
    public int TaskProgress { get; set; }
}

public class ProjectTaskInput : IValidatableObject
{
    [Required]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "任務名稱")]
    public string TaskName { get; set; } = string.Empty;

    [Display(Name = "任務群組")]
    public string? TaskGroup { get; set; }

    [Display(Name = "負責人")]
    public int? ResponsibleId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "計畫開始日")]
    public DateTime? PlanStart { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "計畫結束日")]
    public DateTime? PlanEnd { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlanStart.HasValue && PlanEnd.HasValue && PlanStart > PlanEnd)
        {
            yield return new ValidationResult("計畫開始日期不得晚於結束日期", new[] { nameof(PlanStart), nameof(PlanEnd) });
        }
    }
}

public class ProjectTaskViewModel
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string? TaskGroup { get; set; }
    public string? TaskGroupName { get; set; }
    public int? ResponsibleId { get; set; }
    public string? ResponsibleName { get; set; }
    public DateTime? PlanStart { get; set; }
    public DateTime? PlanEnd { get; set; }
    public int TaskProgress { get; set; }
}
