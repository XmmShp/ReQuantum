namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 日程事件
/// </summary>
public record CalendarEvent(
    long Id,
    string Content,
    DateTime StartTime,
    DateTime EndTime,
    DateTime CreatedAt,
    string From,
    string Note,
    int Source);
