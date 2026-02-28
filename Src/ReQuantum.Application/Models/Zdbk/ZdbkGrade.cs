namespace ReQuantum.Application.Models.Zdbk;

public class ZdbkGrades
{
    public List<ZdbkCoursesGrade> CoursesGrade { get; set; } = new();
    public double Credit { get; set; }
    public double MajorCredit { get; set; }
    public double GradePoint5 { get; set; }
    public double GradePoint4 { get; set; }
    public double GradePoint100 { get; set; }
    public double MajorGradePoint { get; set; }
}

public class ZdbkCoursesGrade
{
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public double Grade100 { get; set; }
    public double Grade5 { get; set; }
    public double Credit { get; set; }
    public string Term { get; set; } = string.Empty;
}
