using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.CoursesZju.Models;
using ReQuantum.Modules.CoursesZju.Services;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.Utilities;
using ReQuantum.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(TodoListViewModel), typeof(IEventHandler<CalendarSelectedDateChanged>)])]
public partial class TodoListViewModel : ViewModelBase<TodoListView>, IEventHandler<CalendarSelectedDateChanged>
{
    private readonly ICalendarService _calendarService;
    private readonly ICoursesZjuService _coursesZjuService;
    private readonly IZjuSsoService _zjuSsoService;

    public string SyncCoursesZjuText => "🔁" + UIText.SyncCoursesZju;
    public string AddTodoText => "➕" + UIText.AddTodo;

    #region 数据集合

    [ObservableProperty]
    private ObservableCollection<CalendarTodo> _todos = [];

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    #endregion

    #region 编辑状态

    [ObservableProperty]
    private string _newTodoContent = string.Empty;

    public DateTime NewTodoDueTime
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                CheckTodoTimeWarning();
            }
        }
    } = DateTime.Now;

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

    [ObservableProperty]
    private bool _isSyncingCoursesZju;

    #endregion

    public TodoListViewModel(
        ICalendarService calendarService,
        ICoursesZjuService coursesZjuService,
        IZjuSsoService zjuSsoService)
    {
        _calendarService = calendarService;
        _coursesZjuService = coursesZjuService;
        _zjuSsoService = zjuSsoService;
        LoadTodos();

        // 如果已登录，自动在后台抓取学在浙大待办
        if (_zjuSsoService.IsAuthenticated)
        {
            _ = SyncCoursesZjuTodosAsync();
        }

        // 订阅登录事件，登录后自动同步
        _zjuSsoService.OnLogin += () =>
        {
            OnPropertyChanged(nameof(ShowCoursesZjuSyncButton));
            _ = SyncCoursesZjuTodosAsync();
        };

        // 订阅登出事件，登出后隐藏按钮
        _zjuSsoService.OnLogout += () =>
        {
            OnPropertyChanged(nameof(ShowCoursesZjuSyncButton));
        };
    }

    #region 数据加载

    public void LoadTodos()
    {
        // 加载待办：
        // 1. 如果选中的是今天或未来：显示截止日期 <= 选中日期 且未完成的待办
        // 2. 如果选中的是过去：只显示截止日期 = 选中日期的待办（无论是否完成）
        var today = DateOnly.FromDateTime(DateTime.Now);

        var todos = _calendarService.GetTodosByDate(SelectedDate);

        if (SelectedDate >= today)
        {
            todos = todos.Union(_calendarService.GetIncompleteTodosByDate(SelectedDate)).ToList();
        }

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

        _calendarService.AddOrUpdateTodo(todo);

        // 如果新待办的截止日期在选中日期之前或等于选中日期，则添加到列表
        if (DateOnly.FromDateTime(todo.DueTime) <= SelectedDate)
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
    }

    [RelayCommand]
    private void UpdateTodo(CalendarTodo todo)
    {
        _calendarService.AddOrUpdateTodo(todo);
        LoadTodos();
    }

    #endregion

    #region 学在浙大待办同步

    /// <summary>
    /// 是否显示学在浙大同步按钮
    /// </summary>
    public bool ShowCoursesZjuSyncButton => _zjuSsoService.IsAuthenticated;

    /// <summary>
    /// 同步学在浙大待办
    /// </summary>
    [RelayCommand]
    private async Task SyncCoursesZjuTodosAsync()
    {
        if (IsSyncingCoursesZju)
        {
            return;
        }

        IsSyncingCoursesZju = true;

        try
        {
            var result = await _coursesZjuService.GetTodoListAsync();
            if (!result.IsSuccess)
            {
                // 同步失败，可以显示错误消息
                return;
            }

            var newTodos = result.Value.Select(CalendarTodo.FromCoursesZjuTodo).ToArray();
            var existingCoursesZjuTodos = _calendarService.GetAllTodos()
                .Where(t => t.IsFromCoursesZju)
                .ToList();

            // 找出已经不存在的待办，标记为已完成
            var newTodoIds = newTodos.Select(t => t.Id).ToHashSet();
            foreach (var existingTodo in existingCoursesZjuTodos.Where(existingTodo => !newTodoIds.Contains(existingTodo.Id) && !existingTodo.IsCompleted))
            {
                existingTodo.IsCompleted = true;
                _calendarService.AddOrUpdateTodo(existingTodo);
            }

            // 添加或更新新的待办
            foreach (var todo in newTodos)
            {
                _calendarService.AddOrUpdateTodo(todo);
            }

            // 刷新列表
            LoadTodos();
        }
        finally
        {
            IsSyncingCoursesZju = false;
        }
    }

    #endregion

    public void Handle(CalendarSelectedDateChanged @event)
    {
        SelectedDate = @event.Date;
    }
}

public static class CalendarTodoExtensions
{
    extension(CalendarTodo todo)
    {
        public static CalendarTodo FromCoursesZjuTodo(CoursesZjuTodoDto czTodo)
        {
            return new CalendarTodo
            {
                Id = czTodo.Id.ToGuid(),
                Content = $"{czTodo.CourseName}\n{czTodo.Title}",
                DueTime = czTodo.EndTime.ToLocalTime(),
                IsCompleted = false,
                IsFromCoursesZju = true
            };
        }
    }
}