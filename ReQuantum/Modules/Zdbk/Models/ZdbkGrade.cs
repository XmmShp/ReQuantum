using System.Collections.Generic;

namespace ReQuantum.Modules.Zdbk.Models;



public class ZdbkGrades
{
    /// <summary>
    /// 课程成绩列表
    /// </summary>
    public List<ZdbkCoursesGrade> CoursesGrade { get; set; }

    /// <summary>
    /// 总学分
    /// </summary>
    public double Credit { get; set; }
    /// <summary>
    /// 主修学分
    /// </summary>
    public double MajorCredit { get; set; }
    /// <summary>
    /// 绩点（五分制）
    /// </summary>
    public double GradePoint5 { get; set; }
    /// <summary>
    /// 绩点（四分制）
    /// </summary>
    public double GradePoint4 { get; set; }
    /// <summary>
    /// 绩点（百分制）
    /// </summary>
    public double GradePoint100 { get; set; }
    /// <summary>
    /// 主修绩点
    /// </summary>
    public double MajorGradePoint { get; set; }
}


public class ZdbkCoursesGrade
{
    /// <summary>
    /// 课程名称
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// 课程代码
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;


    /// <summary>
    /// 百分成绩
    /// </summary>
    public double Grade100 { get; set; }


    /// <summary>
    /// 绩点
    /// </summary>
    public double Grade5 { get; set; }

    /// <summary>
    /// 学分
    /// </summary>
    public double Credit { get; set; }


    /// <summary>
    /// 学期
    /// </summary>
    public string Term { get; set; } = string.Empty;
}

