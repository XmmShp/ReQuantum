namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 日历日期数据
/// </summary>
public record CalendarDayData(
    DateOnly Date,
    List<CalendarTodo> Todos,
    List<CalendarEvent> Events,
    List<CalendarNote> Notes);
