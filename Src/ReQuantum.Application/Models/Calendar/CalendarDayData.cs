namespace ReQuantum.Application.Models.Calendar;

/// <summary>
/// 日历日期数据模型
/// </summary>
public class CalendarDayData
{
    public DateOnly Date { get; set; }
    public List<CalendarTodo> Todos { get; set; } = [];
    public List<CalendarEvent> Events { get; set; } = [];
    public List<CalendarNote> Notes { get; set; } = [];
}
