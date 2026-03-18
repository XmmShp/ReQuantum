namespace ReQuantum.Application.Zdbk.Models;

public class ParsedCourseInfo
{
    public string CourseName { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int WeekStart { get; set; }
    public int WeekEnd { get; set; }
    public DateTime? ExamDate { get; set; }
    public TimeOnly? ExamStartTime { get; set; }
    public TimeOnly? ExamEndTime { get; set; }
    public string RawInfo { get; set; } = string.Empty;
}
