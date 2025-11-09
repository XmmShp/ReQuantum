using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Attributes;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Collections.ObjectModel;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Transient, RegisterTypes = [typeof(TodoListViewModel)])]
public partial class TodoListViewModel : ViewModelBase<TodoListView>
{
    private readonly ICalendarService _calendarService;

    #region 数据集合

    [ObservableProperty]
    private ObservableCollection<CalendarTodo> _todos = [];

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    #endregion

    #region 编辑状态

    [ObservableProperty]
    private string _newTodoContent = string.Empty;

    private DateTime _newTodoDueTime = DateTime.Now;
    public DateTime NewTodoDueTime
    {
        get => _newTodoDueTime;
        set
        {
            if (SetProperty(ref _newTodoDueTime, value))
            {
                CheckTodoTimeWarning();
            }
        }
    }

    // 辅助属性：用于DatePicker绑定
    public DateTimeOffset NewTodoDueDate
    {
        get => new(NewTodoDueTime);
        set
        {
            var time = NewTodoDueTime.TimeOfDay;
            NewTodoDueTime = value.DateTime.Date + time;
        }
    }

    // 辅助属性：用于TimePicker绑定
    public TimeSpan NewTodoDueTimeOfDay
    {
        get => NewTodoDueTime.TimeOfDay;
        set
        {
            var date = NewTodoDueTime.Date;
            NewTodoDueTime = date + value;
        }
    }

    private void CheckTodoTimeWarning()
    {
        if (IsAddDialogOpen && NewTodoDueTime < DateTime.Now)
        {
            WarningMessage = Localizer[nameof(UIText.TodoInPast)];
        }
        else if (IsAddDialogOpen)
        {
            WarningMessage = string.Empty;
        }
    }

    #endregion

    #region 对话框状态

    [ObservableProperty]
    private bool _isAddDialogOpen;

    [ObservableProperty]
    private string _warningMessage = string.Empty;

    #endregion

    public TodoListViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        LoadTodos();
    }

    #region 数据加载

    public void LoadTodos()
    {
        // 加载待办：
        // 1. 如果选中的是今天或未来：显示截止日期 <= 选中日期 且未完成的待办
        // 2. 如果选中的是过去：只显示截止日期 = 选中日期的待办（无论是否完成）
        var today = DateOnly.FromDateTime(DateTime.Now);

        var todos =
            // 今天或未来：显示所有未完成且截止日期 <= 选中日期的待办
            SelectedDate >= today
            ? _calendarService.GetIncompleteTodosByDate(SelectedDate)
            : _calendarService.GetTodosByDate(SelectedDate);

        Todos = new ObservableCollection<CalendarTodo>(todos);
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        LoadTodos();
    }

    #endregion

    #region 待办管理

    [RelayCommand]
    private void ShowAddDialog()
    {
        NewTodoContent = string.Empty;
        WarningMessage = string.Empty;
        // 使用选中日期，时间设置为当前时间
        var now = DateTime.Now;
        NewTodoDueTime = SelectedDate.ToDateTime(new TimeOnly(now.Hour, now.Minute));
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    private void AddTodo()
    {
        if (string.IsNullOrWhiteSpace(NewTodoContent))
        {
            return;
        }

        var todo = new CalendarTodo
        {
            Content = NewTodoContent.Trim(),
            DueTime = NewTodoDueTime
        };

        _calendarService.AddTodo(todo);

        // 如果新待办的截止日期在选中日期之前或等于选中日期，则添加到列表
        if (todo.DueDate <= SelectedDate)
        {
            Todos.Add(todo);
        }

        NewTodoContent = string.Empty;
        NewTodoDueTime = DateTime.Now;
        WarningMessage = string.Empty;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        NewTodoContent = string.Empty;
        NewTodoDueTime = DateTime.Now;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void DeleteTodo(CalendarTodo todo)
    {
        _calendarService.DeleteTodo(todo.Id);
        Todos.Remove(todo);
    }

    [RelayCommand]
    private void ToggleTodoComplete(CalendarTodo todo)
    {
        _calendarService.ToggleTodoComplete(todo.Id);

        // 如果完成了，从列表中移除
        if (todo.IsCompleted)
        {
            Todos.Remove(todo);
        }
    }

    [RelayCommand]
    private void UpdateTodo(CalendarTodo todo)
    {
        _calendarService.UpdateTodo(todo);
        LoadTodos();
    }

    #endregion
}
