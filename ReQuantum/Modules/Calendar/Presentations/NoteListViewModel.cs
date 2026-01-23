using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Views;
using System.Collections.ObjectModel;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(NoteListViewModel)])]
public partial class NoteListViewModel : ViewModelBase<NoteListView>
{
    private readonly ICalendarService _calendarService;


    [ObservableProperty]
    private int _noteId;

    #region 数据集合

    [ObservableProperty]
    private ObservableCollection<CalendarNote> _notes = [];

    #endregion

    #region 编辑状态

    [ObservableProperty]
    private string _newNoteContent = string.Empty;

    #endregion

    #region 对话框状态

    [ObservableProperty]
    private bool _isAddDialogOpen;

    #endregion

    public NoteListViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        LoadNotes();
    }

    #region 数据加载

    public void LoadNotes()
    {
        var notes = _calendarService.GetAllNotes();

        for (int i = 0; i < notes.Count; i++)
        {
            notes[i].NoteId = i;
        }
        Notes = new ObservableCollection<CalendarNote>(notes);
    }

    #endregion

    #region 便签管理

    [RelayCommand]
    private void ShowAddDialog()
    {
        NewNoteContent = string.Empty;
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    private void AddNote()
    {
        if (string.IsNullOrWhiteSpace(NewNoteContent))
        {
            return;
        }

        var note = new CalendarNote
        {
            Content = NewNoteContent.Trim()
        };

        _calendarService.AddOrUpdateNote(note);
        Notes.Add(note);
        UpdateNoteSequence();
        NewNoteContent = string.Empty;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        NewNoteContent = string.Empty;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void DeleteNote(CalendarNote note)
    {
        _calendarService.DeleteNote(note.Id);
        UpdateNoteSequence();
        Notes.Remove(note);
    }

    [RelayCommand]
    private void UpdateNote(CalendarNote note)
    {
        _calendarService.AddOrUpdateNote(note);
    }

    #endregion

    private void UpdateNoteSequence()
    {
        for (int i = 0; i < Notes.Count; i++)
        {
            Notes[i].NoteId = i;
        }
    }
}
