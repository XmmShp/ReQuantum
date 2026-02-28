using System.Text.Json.Serialization;
using ReQuantum.Application.Constants;
using ReQuantum.Application.Parsers;

namespace ReQuantum.Application.Models.Zdbk;

public class ZdbkSectionDto
{
    [JsonPropertyName("xkkh")]
    public string CourseId { get; set; } = string.Empty;

    [JsonPropertyName("kcb")]
    public string CourseInfo { get; set; } = string.Empty;

    [JsonPropertyName("xqj")]
    public string DayOfWeek { get; set; } = string.Empty;

    [JsonPropertyName("djj")]
    public string StartSection { get; set; } = string.Empty;

    [JsonPropertyName("skcd")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("dsz")]
    public string WeekType { get; set; } = string.Empty;

    [JsonPropertyName("xxq")]
    public string Term { get; set; } = string.Empty;

    [JsonPropertyName("jszgh")]
    public string? TeacherId { get; set; }

    public (ParsedCourseInfo Info, TimeOnly StartTime, TimeOnly EndTime) Parse()
    {
        var courseInfo = CourseInfoParser.Parse(CourseInfo);
        var (startTime, endTime) = ClassTimeTable.GetClassTime(int.Parse(StartSection), int.Parse(Duration));
        return (courseInfo, startTime, endTime);
    }
}
