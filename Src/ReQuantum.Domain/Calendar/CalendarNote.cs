namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 便签 - 只有内容，没有日期
/// </summary>
public class CalendarNote
{
    public CalendarNoteId Id { get; private set; }
    public int NoteId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private CalendarNote() { }

    public static CalendarNote Create(string content)
    {
        return new CalendarNote
        {
            Id = CalendarNoteId.New(),
            Content = content,
            CreatedAt = DateTime.Now
        };
    }

    public void Update(string content)
    {
        Content = content;
    }
}
