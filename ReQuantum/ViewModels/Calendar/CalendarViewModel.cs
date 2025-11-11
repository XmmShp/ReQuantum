using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Material;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Views;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(CalendarViewModel), typeof(IMenuItemProvider)])]
public partial class CalendarViewModel : ViewModelBase<CalendarView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public string Title => UIText.Calendar;
    public PackIconMaterialKind IconKind => PackIconMaterialKind.Calendar;
    public uint Order => 2;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(CalendarViewModel);
    Action<MenuItem> IMenuItemProvider.OnCultureChanged => item => item.Title = UIText.Calendar;
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
        CalendarPartViewModel = calendarPartViewModel;
        TodoListViewModel = todoListViewModel;
        EventListViewModel = eventListViewModel;
        NoteListViewModel = noteListViewModel;

        // 订阅日历部分的日期变化，同步到其他组件
        CalendarPartViewModel.DateSelected += (s, e) =>
        {
            TodoListViewModel.SelectedDate = e;
            EventListViewModel.SelectedDate = e;
        };
    }
}
