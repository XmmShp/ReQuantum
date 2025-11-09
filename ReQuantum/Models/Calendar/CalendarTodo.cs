using System;

namespace ReQuantum.Models;

/// <summary>
/// 待办 - 有截止时间和内容
/// </summary>
public class CalendarTodo
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime DueTime { get; set; } // 截止时间（包含日期和时间）
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public CalendarTodo()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        DueTime = DateTime.Now;
    }

    // 便捷属性：获取截止日期
    public DateOnly DueDate => DateOnly.FromDateTime(DueTime);
}
