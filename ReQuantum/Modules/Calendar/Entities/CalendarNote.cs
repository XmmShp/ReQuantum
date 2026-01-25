using System;

namespace ReQuantum.Modules.Calendar.Entities;

/// <summary>
/// 便签 - 只有内容，没有日期
/// </summary>
public class CalendarNote
{
    public Guid Id { get; set; }
    public int NoteId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public CalendarNote()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
    }
}
