using System;

namespace ReQuantum.Modules.Calendar.Entities;

/// <summary>
/// 日程 - 有开始时间、结束时间和内容
/// </summary>
public class CalendarEvent
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public string From { get; set; } = string.Empty;
    
    // 标识是否来自教务网课程表
    public bool IsFromZdbk { get; set; }

    // 标识是否来自教务网考试
    public bool IsFromZdbkExam { get; set; }

    // 标识是否来自 PTA
    public bool IsFromPta { get; set; }

    public CalendarEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        StartTime = DateTime.Now;
        EndTime = DateTime.Now.AddHours(1); // 默认1小时
    }
}
