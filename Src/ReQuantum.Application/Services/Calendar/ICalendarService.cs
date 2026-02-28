using ReQuantum.Application.Models.Calendar;

namespace ReQuantum.Application.Services.Calendar;

public interface ICalendarService
{
    List<CalendarNote> GetAllNotes();
    void AddOrUpdateNote(CalendarNote note);
    void DeleteNote(Guid id);

    List<CalendarTodo> GetAllTodos();
    List<CalendarTodo> GetTodosByDate(DateOnly date);
    List<CalendarTodo> GetTodosByDateRange(DateOnly startDate, DateOnly endDate);
    List<CalendarTodo> GetIncompleteTodosByDate(DateOnly date);
    void AddOrUpdateTodo(CalendarTodo todo);
    void DeleteTodo(Guid id);
    void ToggleTodoComplete(Guid id);

    List<CalendarEvent> GetAllEvents();
    List<CalendarEvent> GetEventsByDate(DateOnly date);
    List<CalendarEvent> GetEventsByDateRange(DateOnly startDate, DateOnly endDate);
    void AddOrUpdateEvent(CalendarEvent calendarEvent);
    void DeleteEvent(Guid id);

    CalendarDayData GetCalendarDayData(DateOnly date);
}
