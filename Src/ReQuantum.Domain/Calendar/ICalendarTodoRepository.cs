namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 待办事项仓储接口
/// </summary>
public interface ICalendarTodoRepository
{
    List<CalendarTodo> FindAll();
    List<CalendarTodo> FindByDate(DateOnly date);
    List<CalendarTodo> FindByDateRange(DateOnly startDate, DateOnly endDate);
    List<CalendarTodo> FindIncompleteByDate(DateOnly date);
    CalendarTodo? FindById(CalendarTodoId id);
    void AddOrUpdate(CalendarTodo todo);
    void Delete(CalendarTodoId id);
}
