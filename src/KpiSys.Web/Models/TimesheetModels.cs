using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace KpiSys.Web.Models;

public class TimesheetEntry
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "專案")]
    public string ProjectCode { get; set; } = string.Empty;

    [Display(Name = "任務")]
    public int? TaskId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "工作日期")]
    public DateTime WorkDate { get; set; }

    [Range(0, 24, ErrorMessage = "工時需介於 0 到 24 小時")]
    [Display(Name = "工時")]
    public decimal Hours { get; set; }

    [Range(0, 24, ErrorMessage = "加班需介於 0 到 24 小時")]
    [Display(Name = "加班")]
    public decimal OvertimeHours { get; set; }

    [Display(Name = "狀態")]
    public string Status { get; set; } = "Draft";

    public DateTime? SubmittedAt { get; set; }
}

public class TimesheetAudit
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
}

public class TimesheetListItem
{
    public int Id { get; set; }
    public DateTime WorkDate { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? TaskName { get; set; }
    public decimal Hours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CanSubmit { get; set; }

    public decimal TotalHours => Hours + OvertimeHours;
}

public class TimesheetListViewModel
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public List<TimesheetListItem> Entries { get; set; } = new();
    public decimal TotalHours => Entries.Sum(e => e.TotalHours);
}

public class TimesheetProjectOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TimesheetTaskOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TimesheetFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "工作日期")]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "專案")]
    public string ProjectCode { get; set; } = string.Empty;

    [Display(Name = "任務")]
    public int? TaskId { get; set; }

    [Range(0, 24, ErrorMessage = "工時需介於 0 到 24 小時")]
    [Display(Name = "工時")]
    public decimal Hours { get; set; }

    [Range(0, 24, ErrorMessage = "加班需介於 0 到 24 小時")]
    [Display(Name = "加班")]
    public decimal OvertimeHours { get; set; }

    public string Status { get; set; } = "Draft";

    public bool IsEdit => Id.HasValue;

    public List<TimesheetProjectOption> ProjectOptions { get; set; } = new();
    public List<TimesheetTaskOption> TaskOptions { get; set; } = new();
    public Dictionary<string, List<TimesheetTaskOption>> ProjectTasks { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (WorkDate.Date > DateTime.Today)
        {
            yield return new ValidationResult("工作日期不可為未來日", new[] { nameof(WorkDate) });
        }

        if (Hours == 0 && OvertimeHours == 0)
        {
            yield return new ValidationResult("工時與加班不可同時為 0", new[] { nameof(Hours), nameof(OvertimeHours) });
        }
    }
}
