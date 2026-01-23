namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 表示选课界面可选择的课程（继承自 StatefulCourse，添加选课课号）
/// </summary>
public class SelectableCourse : StatefulCourse
{
    /// <summary>
    /// 选课课号（用于选课系统的唯一标识）
    /// </summary>
    public required string Code { get; init; }
}