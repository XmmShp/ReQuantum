using ReQuantum.Modules.Zdbk.Enums;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 带状态的课程（继承自 Course，添加选课状态）
/// </summary>
public class StatefulCourse : Course
{
    public required CourseStatus Status { get; set; }
}