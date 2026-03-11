namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 便签
/// </summary>
public record CalendarNote(
    long Id,
    string Content,
    DateTime CreatedAt);
