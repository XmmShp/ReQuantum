using NOF.Annotation;
using ReQuantum.Domain.Calendar;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Repositories.Calendar;

/// <summary>
/// 基于 IStorage 的便签仓储实现
/// </summary>
[AutoInject(Lifetime.Singleton)]
public class StorageCalendarNoteRepository : ICalendarNoteRepository
{
    private readonly IStorage _storage;
    private const string StorageKey = "Calendar:Notes";
    private List<CalendarNote> _notes;

    public StorageCalendarNoteRepository(IStorage storage)
    {
        _storage = storage;
        _notes = _storage.TryGet<List<CalendarNote>>(StorageKey, out var notes) && notes is not null
            ? notes
            : [];
    }

    public List<CalendarNote> FindAll() => _notes.ToList();

    public CalendarNote? FindById(CalendarNoteId id) =>
        _notes.FirstOrDefault(n => n.Id == id);

    public void AddOrUpdate(CalendarNote note)
    {
        var index = _notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0)
        {
            _notes[index] = note;
        }
        else
        {
            _notes.Add(note);
        }

        Save();
    }

    public void Delete(CalendarNoteId id)
    {
        _notes.RemoveAll(n => n.Id == id);
        Save();
    }

    private void Save() => _storage.Set(StorageKey, _notes);
}
