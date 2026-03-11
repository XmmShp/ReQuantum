using NOF.Annotation;
using ReQuantum.Domain.Calendar;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Repositories.Calendar;

/// <summary>
/// 基于 IStorage 的日程事件仓储实现
/// </summary>
[AutoInject(Lifetime.Singleton)]
public class StorageCalendarEventRepository : ICalendarEventRepository
{
    private readonly IStorage _storage;
    private const string StorageKey = "Calendar:Events";
    private List<CalendarEvent> _events;

    public StorageCalendarEventRepository(IStorage storage)
    {
        _storage = storage;
        _events = _storage.TryGet<List<CalendarEvent>>(StorageKey, out var events) && events is not null
            ? events
            : [];
    }

    public List<CalendarEvent> FindAll() => _events.ToList();

    public List<CalendarEvent> FindByDate(DateOnly date) =>
        _events
            .Where(e => DateOnly.FromDateTime(e.StartTime) <= date && DateOnly.FromDateTime(e.EndTime) >= date)
            .OrderBy(e => e.StartTime)
            .ToList();

    public List<CalendarEvent> FindByDateRange(DateOnly startDate, DateOnly endDate) =>
        _events
            .Where(e => DateOnly.FromDateTime(e.StartTime) >= startDate && DateOnly.FromDateTime(e.StartTime) <= endDate)
            .OrderBy(e => e.StartTime)
            .ToList();

    public CalendarEvent? FindById(CalendarEventId id) =>
        _events.FirstOrDefault(e => e.Id == id);

    public void AddOrUpdate(CalendarEvent calendarEvent)
    {
        var index = _events.FindIndex(e => e.Id == calendarEvent.Id);
        if (index >= 0)
        {
            _events[index] = calendarEvent;
        }
        else
        {
            _events.Add(calendarEvent);
        }

        Save();
    }

    public void Delete(CalendarEventId id)
    {
        _events.RemoveAll(e => e.Id == id);
        Save();
    }

    private void Save() => _storage.Set(StorageKey, _events);
}
