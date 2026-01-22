using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Models;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.ViewModels;
using ReQuantum.Views;

namespace ReQuantum.Modules.Calendar.Presentations;

/// <summary>
/// 周视图日历ViewModel
/// </summary>
[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(WeekCalendarViewModel), typeof(IEventHandler<CalendarSelectedDateChanged>)])]
public partial class WeekCalendarViewModel : ViewModelBase<WeekCalendarView>, IEventHandler<CalendarSelectedDateChanged>
{
    private readonly ICalendarService _calendarService;

    // 静态成员，确保全局唯一
    private static readonly Dictionary<DateOnly, WeekDay> WeekDayCache = [];
    private WeekDay? _previousSelectedDay;

    public WeekCalendarViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        UpdateWeek();
    }

    [ObservableProperty]
    private DateOnly _weekStartDate = DateOnly.FromDateTime(DateTime.Now);

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    [ObservableProperty]
    private ObservableCollection<WeekDay> _weekDays = [];

    partial void OnWeekStartDateChanged(DateOnly value)
    {
        UpdateWeek();
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        // 取消上一个选中的日期
        _previousSelectedDay?.IsSelected = false;

        var newSelectedDay = GetOrCreateWeekDay(value);
        newSelectedDay.IsSelected = true;
        _previousSelectedDay = newSelectedDay;
    }

    /// <summary>
    /// 获取或创建 WeekDay（全局唯一，带缓存）
    /// </summary>
    private WeekDay GetOrCreateWeekDay(DateOnly date)
    {
        if (WeekDayCache.TryGetValue(date, out var existingDay))
        {
            return existingDay;
        }

        // 创建新的 WeekDay
        var dayData = _calendarService.GetCalendarDayData(date);
        var weekDay = new WeekDay(dayData, this);

        WeekDayCache[date] = weekDay;
        return weekDay;
    }

    private void UpdateWeek()
    {
        var days = new List<WeekDay>();
        var today = DateOnly.FromDateTime(DateTime.Now);

        var todayDay = GetOrCreateWeekDay(today);
        todayDay.IsToday = true;

        // 生成一周的7天
        for (var i = 0; i < 7; i++)
        {
            var date = WeekStartDate.AddDays(i);
            var weekDay = GetOrCreateWeekDay(date);
            weekDay.IsSelected = false;
            days.Add(weekDay);
        }

        _previousSelectedDay?.IsSelected = true;

        WeekDays = new ObservableCollection<WeekDay>(days);
    }

    public void SelectDate(DateOnly date)
    {
        SelectedDate = date;
        Publisher.Publish(new CalendarSelectedDateChanged(date));
    }

    public void Handle(CalendarSelectedDateChanged @event)
    {
        SelectedDate = @event.Date;
    }
}

public partial class WeekDay : ObservableObject
{
    private readonly CalendarDayData _dayData;
    private readonly WeekCalendarViewModel _viewModel;

    public WeekDay(CalendarDayData dayData, WeekCalendarViewModel viewModel)
    {
        _dayData = dayData;
        _viewModel = viewModel;
        Date = dayData.Date;
        Day = dayData.Date.Day;
        DayOfWeek = dayData.Date.DayOfWeek.ToString()[..3];

        // 监听 CalendarDayData 的 Todos 和 Events 变化
        _dayData.Todos.CollectionChanged += OnDataCollectionChanged;
        _dayData.Events.CollectionChanged += OnDataCollectionChanged;

        // 初始化 TimelineItems
        UpdateTimelineItems();
    }

    public DateOnly Date { get; }
    public int Day { get; }
    public string DayOfWeek { get; }

    [ObservableProperty]
    private bool _isToday;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private List<WeekTimelineItem> _timelineItems = [];

    /// <summary>
    /// 当数据集合变化时，更新显示的 TimelineItems
    /// </summary>
    private void OnDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateTimelineItems();
    }

    /// <summary>
    /// 从 CalendarDayData 生成显示用的 TimelineItems
    /// </summary>
    private void UpdateTimelineItems()
    {
        TimelineItems = CalendarItemsHelper.GenerateWeekViewItems(
            _dayData.Todos.ToList(),
            _dayData.Events.ToList());
    }

    /// <summary>
    /// 选中当前日期的命令
    /// </summary>
    [RelayCommand]
    private void Select()
    {
        _viewModel.SelectDate(Date);
    }
}

/// <summary>
/// 周视图时间线事项
/// </summary>
public partial class WeekTimelineItem : ObservableObject
{
    [ObservableProperty]
    private bool _isTodo;

    [ObservableProperty]
    private bool _isEvent;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private double _topPosition; // 在时间线上的位置（0-24小时）

    [ObservableProperty]
    private double _height; // 高度（小时数）

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private string _timeLabel = string.Empty; // 时间标注（例如："09:00 - 10:30"）
}
