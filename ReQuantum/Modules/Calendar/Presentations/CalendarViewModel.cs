using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Material;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Presentations;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using ReQuantum.Services;
using ReQuantum.Views;
using System;

namespace ReQuantum.ViewModels;

public record CalendarSelectedDateChanged(DateOnly Date) : IEvent;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(CalendarViewModel), typeof(IMenuItemProvider)])]
public partial class CalendarViewModel : ViewModelBase<CalendarView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public MenuItem MenuItem { get; }
    public uint Order => 2;
    #endregion

    [ObservableProperty]
    private CalendarPartViewModel _calendarPartViewModel;

    [ObservableProperty]
    private TodoListViewModel _todoListViewModel;

    [ObservableProperty]
    private EventListViewModel _eventListViewModel;

    [ObservableProperty]
    private NoteListViewModel _noteListViewModel;

    public CalendarViewModel(
        CalendarPartViewModel calendarPartViewModel,
        TodoListViewModel todoListViewModel,
        EventListViewModel eventListViewModel,
        NoteListViewModel noteListViewModel)
    {
        MenuItem = new MenuItem
        {
            Title = new LocalizedText { Key = nameof(UIText.Calendar) },
            IconKind = PackIconMaterialKind.Calendar,
            OnSelected = () => Navigator.NavigateTo<CalendarViewModel>()
        };
        CalendarPartViewModel = calendarPartViewModel;
        TodoListViewModel = todoListViewModel;
        EventListViewModel = eventListViewModel;
        NoteListViewModel = noteListViewModel;
    }
}
