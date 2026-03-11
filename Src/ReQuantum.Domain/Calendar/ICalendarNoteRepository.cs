namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 便签仓储接口
/// </summary>
public interface ICalendarNoteRepository
{
    List<CalendarNote> FindAll();
    CalendarNote? FindById(CalendarNoteId id);
    void AddOrUpdate(CalendarNote note);
    void Delete(CalendarNoteId id);
}
