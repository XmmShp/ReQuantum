namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 日程事件仓储接口
/// </summary>
public interface ICalendarEventRepository
{
    List<CalendarEvent> FindAll();
    List<CalendarEvent> FindByDate(DateOnly date);
    List<CalendarEvent> FindByDateRange(DateOnly startDate, DateOnly endDate);
    CalendarEvent? FindById(CalendarEventId id);
    void AddOrUpdate(CalendarEvent calendarEvent);
    void Delete(CalendarEventId id);
}
