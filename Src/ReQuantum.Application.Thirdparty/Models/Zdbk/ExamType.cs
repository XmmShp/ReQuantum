namespace ReQuantum.Application.Models.Zdbk;

public enum ExamType
{
    MidTerm,
    FinalTerm,
    NoExam
}

public class ParsedExamInfo
{
    public string ClassId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public float Credit { get; set; }
    public ExamType ExamType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
    public string? Seat { get; set; }
    public string? RawTimeString { get; set; }
}
