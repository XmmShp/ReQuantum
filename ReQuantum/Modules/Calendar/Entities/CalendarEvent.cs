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
    public string Note { get; set; } = string.Empty;

    public CalendarEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        StartTime = DateTime.Now;
        EndTime = DateTime.Now.AddHours(1); // 默认1小时
    }
}
