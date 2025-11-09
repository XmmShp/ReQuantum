using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Attributes;
using ReQuantum.Infrastructure;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Collections.ObjectModel;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Transient, RegisterTypes = [typeof(EventListViewModel)])]
public partial class EventListViewModel : ViewModelBase<EventListView>
{
    private readonly ICalendarService _calendarService;

    /// <summary>
    /// 动态标题：日程 - 日期
    /// </summary>
    public LocalizedText EventsTitle { get; }

    #region 数据集合

    [ObservableProperty]
    private ObservableCollection<CalendarEvent> _events = [];

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    #endregion

    #region 编辑状态

    [ObservableProperty]
    private string _newEventTitle = string.Empty;

    [ObservableProperty]
    private string _newEventContent = string.Empty;

    private DateTime _newEventStartTime = DateTime.Now;
    public DateTime NewEventStartTime
    {
        get => _newEventStartTime;
        set
        {
            if (SetProperty(ref _newEventStartTime, value))
            {
                CheckEventTimeWarning();
            }
        }
    }

    // 辅助属性：用于DatePicker绑定
    public DateTimeOffset NewEventStartDate
    {
        get => new(NewEventStartTime);
        set
        {
            var time = NewEventStartTime.TimeOfDay;
            NewEventStartTime = value.DateTime.Date + time;
        }
    }

    // 辅助属性：用于TimePicker绑定
    public TimeSpan NewEventStartTimeOfDay
    {
        get => NewEventStartTime.TimeOfDay;
        set
        {
            var date = NewEventStartTime.Date;
            NewEventStartTime = date + value;
        }
    }

    private DateTime _newEventEndTime = DateTime.Now.AddHours(1);
    public DateTime NewEventEndTime
    {
        get => _newEventEndTime;
        set
        {
            if (SetProperty(ref _newEventEndTime, value))
            {
                CheckEventTimeWarning();
            }
        }
    }

    // 辅助属性：用于DatePicker绑定
    public DateTimeOffset NewEventEndDate
    {
        get => new(NewEventEndTime);
        set
        {
            var time = NewEventEndTime.TimeOfDay;
            NewEventEndTime = value.DateTime.Date + time;
        }
    }

    // 辅助属性：用于TimePicker绑定
    public TimeSpan NewEventEndTimeOfDay
    {
        get => NewEventEndTime.TimeOfDay;
        set
        {
            var date = NewEventEndTime.Date;
            NewEventEndTime = date + value;
        }
    }

    private void CheckEventTimeWarning()
    {
        if (!IsAddDialogOpen)
        { return; }

        // 检查结束时间是否早于开始时间
        if (NewEventEndTime <= NewEventStartTime)
        {
            WarningMessage = Localizer[nameof(UIText.EventEndTimeBeforeStart)];
        }
        // 检查是否创建过去的日程
        else if (NewEventStartTime < DateTime.Now)
        {
            WarningMessage = Localizer[nameof(UIText.EventInPast)];
        }
        else
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

    public EventListViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        EventsTitle = new LocalizedText(Localizer);
        UpdateEventsTitle();
        LoadEvents();
    }

    #region 数据加载

    public void LoadEvents()
    {
        // 加载选中日期的日程（跨越该日期的所有日程）
        var events = _calendarService.GetEventsByDate(SelectedDate);
        Events = new ObservableCollection<CalendarEvent>(events);
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        UpdateEventsTitle();
        LoadEvents();
    }

    private void UpdateEventsTitle()
    {
        EventsTitle.Set(nameof(UIText.EventsOnDate), SelectedDate.ToDateTime(TimeOnly.MinValue));
    }

    #endregion

    #region 日程管理

    [RelayCommand]
    private void ShowAddDialog()
    {
        NewEventTitle = string.Empty;
        NewEventContent = string.Empty;
        WarningMessage = string.Empty;
        // 使用选中日期，时间设置为当前时间
        var now = DateTime.Now;
        NewEventStartTime = SelectedDate.ToDateTime(new TimeOnly(now.Hour, now.Minute));
        NewEventEndTime = NewEventStartTime.AddHours(1);
        IsAddDialogOpen = true;
    }

    [RelayCommand]
    private void AddEvent()
    {
        if (string.IsNullOrWhiteSpace(NewEventTitle))
        {
            return;
        }

        // 验证结束时间必须晚于开始时间
        if (NewEventEndTime <= NewEventStartTime)
        {
            return; // 警告已经实时显示，直接返回
        }

        var calendarEvent = new CalendarEvent
        {
            Title = NewEventTitle.Trim(),
            Content = NewEventContent.Trim(),
            StartTime = NewEventStartTime,
            EndTime = NewEventEndTime
        };

        _calendarService.AddEvent(calendarEvent);

        // 如果新日程的日期是选中日期，则添加到列表
        if (DateOnly.FromDateTime(calendarEvent.StartTime) == SelectedDate)
        {
            Events.Add(calendarEvent);
        }

        NewEventTitle = string.Empty;
        NewEventContent = string.Empty;
        NewEventStartTime = DateTime.Now;
        NewEventEndTime = DateTime.Now.AddHours(1);
        WarningMessage = string.Empty;
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        NewEventTitle = string.Empty;
        NewEventContent = string.Empty;
        NewEventStartTime = DateTime.Now;
        NewEventEndTime = DateTime.Now.AddHours(1);
        IsAddDialogOpen = false;
    }

    [RelayCommand]
    private void DeleteEvent(CalendarEvent calendarEvent)
    {
        _calendarService.DeleteEvent(calendarEvent.Id);
        Events.Remove(calendarEvent);
    }

    [RelayCommand]
    private void UpdateEvent(CalendarEvent calendarEvent)
    {
        _calendarService.UpdateEvent(calendarEvent);
        LoadEvents();
    }

    #endregion
}
