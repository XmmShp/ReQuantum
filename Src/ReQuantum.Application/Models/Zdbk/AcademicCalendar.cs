using System.Text.Json.Serialization;

namespace ReQuantum.Application.Models.Zdbk;

/// <summary>
/// 校历信息
/// </summary>
public class AcademicCalendar
{
    [JsonPropertyName("semester_name")]
    public required string SemesterName { get; set; }

    [JsonPropertyName("start_date")]
    public required DateOnly StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public required DateOnly EndDate { get; set; }

    [JsonPropertyName("course_adjustments")]
    public List<CourseAdjustment> CourseAdjustments { get; set; } = [];

    [JsonPropertyName("class_suspension_dates")]
    public List<DateOnly> ClassSuspensionDates { get; set; } = [];

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("is_short_semester")]
    public bool IsShortSemester { get; set; }

    [JsonIgnore]
    public string AcademicYear
    {
        get
        {
            var parts = SemesterName.Split('-');
            return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : string.Empty;
        }
    }

    [JsonIgnore]
    public string SemesterCode
    {
        get
        {
            var parts = SemesterName.Split('-');
            return parts.Length >= 3 ? parts[2] : "1";
        }
    }

    public int? GetWeekNumber(DateOnly date)
    {
        if (date < StartDate || date > EndDate)
        {
            return null;
        }

        var daysDiff = date.DayNumber - StartDate.DayNumber;
        return (daysDiff / 7) + 1;
    }

    public string GetSemesterNameForWeek(int weekNumber)
    {
        if (IsShortSemester)
        {
            return "夏";
        }

        if (SemesterCode == "1")
        {
            return weekNumber <= 8 ? "秋" : "冬";
        }

        return weekNumber <= 8 ? "春" : "夏";
    }

    public CourseAdjustment? GetAdjustment(DateOnly date) =>
        CourseAdjustments.FirstOrDefault(a => a.OriginalDate == date);

    public bool IsSuspended(DateOnly date) =>
        ClassSuspensionDates.Contains(date);

    public DateOnly GetActualCourseDate(DateOnly date)
    {
        var adjustmentToThisDate = CourseAdjustments.FirstOrDefault(a => a.TargetDate == date);
        if (adjustmentToThisDate != null)
        {
            return adjustmentToThisDate.OriginalDate;
        }

        var adjustmentFromThisDate = CourseAdjustments.FirstOrDefault(a => a.OriginalDate == date);
        if (adjustmentFromThisDate != null)
        {
            return adjustmentFromThisDate.TargetDate;
        }

        return date;
    }
}

public class CourseAdjustment
{
    [JsonPropertyName("original_date")]
    public required DateOnly OriginalDate { get; set; }

    [JsonPropertyName("target_date")]
    public required DateOnly TargetDate { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class AcademicCalendarResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public AcademicCalendar? Data { get; set; }
}
