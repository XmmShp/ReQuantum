using ContractCalendarEvent = ReQuantum.Contract.Calendar.CalendarEvent;
using ContractCalendarTodo = ReQuantum.Contract.Calendar.CalendarTodo;
using ContractCalendarNote = ReQuantum.Contract.Calendar.CalendarNote;
using ContractCalendarDayData = ReQuantum.Contract.Calendar.CalendarDayData;

namespace ReQuantum.Application.Services.Calendar;

public interface ICalendarService
{
    List<ContractCalendarNote> FindAllNotes();
    void AddOrUpdateNote(ContractCalendarNote note);
    void DeleteNote(long id);

    List<ContractCalendarTodo> FindAllTodos();
    List<ContractCalendarTodo> FindTodosByDate(DateOnly date);
    List<ContractCalendarTodo> FindTodosByDateRange(DateOnly startDate, DateOnly endDate);
    List<ContractCalendarTodo> FindIncompleteTodosByDate(DateOnly date);
    void AddOrUpdateTodo(ContractCalendarTodo todo);
    void DeleteTodo(long id);
    void ToggleTodoComplete(long id);

    List<ContractCalendarEvent> FindAllEvents();
    List<ContractCalendarEvent> FindEventsByDate(DateOnly date);
    List<ContractCalendarEvent> FindEventsByDateRange(DateOnly startDate, DateOnly endDate);
    void AddOrUpdateEvent(ContractCalendarEvent calendarEvent);
    void DeleteEvent(long id);

    ContractCalendarDayData FindCalendarDayData(DateOnly date);
}
