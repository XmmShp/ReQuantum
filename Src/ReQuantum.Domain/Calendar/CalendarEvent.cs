namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 日程事件 - 有开始时间、结束时间和内容
/// </summary>
public class CalendarEvent
{
    public CalendarEventId Id { get; private set; }
    public int EventId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string From { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public CalendarEventSource Source { get; private set; }

    private CalendarEvent() { }

    /// <summary>
    /// 创建手动日程事件
    /// </summary>
    public static CalendarEvent Create(string content, DateTime startTime, DateTime endTime, string note = "")
    {
        return new CalendarEvent
        {
            Id = CalendarEventId.New(),
            Content = content,
            StartTime = startTime,
            EndTime = endTime,
            Note = note,
            Source = CalendarEventSource.Manual,
            CreatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建来自外部源的日程事件（教务网、PTA等）
    /// </summary>
    public static CalendarEvent CreateFromSource(
        string content,
        DateTime startTime,
        DateTime endTime,
        CalendarEventSource source,
        string from = "")
    {
        return new CalendarEvent
        {
            Id = CalendarEventId.New(),
            Content = content,
            StartTime = startTime,
            EndTime = endTime,
            Source = source,
            From = from,
            CreatedAt = DateTime.Now
        };
    }

    public void Update(string content, DateTime startTime, DateTime endTime, string note)
    {
        Content = content;
        StartTime = startTime;
        EndTime = endTime;
        Note = note;
    }
}
