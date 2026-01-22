using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Models;
using ReQuantum.Modules.Calendar.Presentations;
using ReQuantum.Modules.Calendar.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Views;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(MonthCalendarViewModel), typeof(IEventHandler<CalendarSelectedDateChanged>)])]
public partial class MonthCalendarViewModel : ViewModelBase<MonthCalendarView>, IEventHandler<CalendarSelectedDateChanged>
{
    private readonly ICalendarService _calendarService;

    // 静态成员，确保全局唯一
    private static readonly ConcurrentDictionary<DateOnly, CalendarDay> CalendarDayCache = [];
    private CalendarDay? _previousSelectedDay;

    public MonthCalendarViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        UpdateCalendar();
    }

    [ObservableProperty]
    private int _year = DateTime.Now.Year;

    [ObservableProperty]
    private int _month = DateTime.Now.Month;

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    [ObservableProperty]
    private ObservableCollection<CalendarDay> _calendarDays = [];

    partial void OnYearChanged(int value)
    {
        UpdateCalendar();
    }

    partial void OnMonthChanged(int value)
    {
        UpdateCalendar();
    }

    partial void OnSelectedDateChanged(DateOnly value)
    {
        _previousSelectedDay?.IsSelected = false;

        var current = GetOrCreateCalendarDay(value);
        _previousSelectedDay = current;
        current.IsSelected = true;
    }

    /// <summary>
    /// 获取或创建 CalendarDay（全局唯一，带缓存）
    /// </summary>
    private CalendarDay GetOrCreateCalendarDay(DateOnly date)
    {
        if (CalendarDayCache.TryGetValue(date, out var existingDay))
        {
            return existingDay;
        }

        // 创建新的 CalendarDay
        var dayData = _calendarService.GetCalendarDayData(date);
        var calendarDay = new CalendarDay(dayData, this);

        CalendarDayCache[date] = calendarDay;
        return calendarDay;
    }

    private void UpdateCalendar()
    {
        var days = new List<CalendarDay>();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var firstDay = new DateOnly(Year, Month, 1);
        var firstDayOfWeek = (int)firstDay.DayOfWeek;

        var todayDay = GetOrCreateCalendarDay(today);
        todayDay.IsToday = true;

        // 添加上个月的日期（前置填充）
        if (firstDayOfWeek > 0)
        {
            var prevMonth = Month == 1 ? 12 : Month - 1;
            var prevYear = Month == 1 ? Year - 1 : Year;
            var daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

            for (var i = firstDayOfWeek - 1; i >= 0; i--)
            {
                var day = daysInPrevMonth - i;
                var date = new DateOnly(prevYear, prevMonth, day);
                var calendarDay = GetOrCreateCalendarDay(date);
                calendarDay.IsCurrentMonth = false;
                calendarDay.IsSelected = calendarDay.Date == SelectedDate;
                days.Add(calendarDay);
            }
        }

        // 添加本月的日期
        var daysInMonth = DateTime.DaysInMonth(Year, Month);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(Year, Month, day);
            var calendarDay = GetOrCreateCalendarDay(date);
            calendarDay.IsCurrentMonth = true;
            calendarDay.IsSelected = calendarDay.Date == SelectedDate;
            days.Add(calendarDay);
        }

        // 添加下个月的日期（后置填充，确保总共42天）
        var remainingDays = 42 - days.Count;
        var nextMonth = Month == 12 ? 1 : Month + 1;
        var nextYear = Month == 12 ? Year + 1 : Year;

        for (var day = 1; day <= remainingDays; day++)
        {
            var date = new DateOnly(nextYear, nextMonth, day);
            var calendarDay = GetOrCreateCalendarDay(date);
            calendarDay.IsCurrentMonth = false;
            calendarDay.IsSelected = calendarDay.Date == SelectedDate;
            days.Add(calendarDay);
        }

        CalendarDays = new ObservableCollection<CalendarDay>(days);
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

public partial class CalendarDay : ObservableObject
{
    private readonly CalendarDayData _dayData;
    private readonly MonthCalendarViewModel _viewModel;

    public CalendarDay(CalendarDayData dayData, MonthCalendarViewModel viewModel)
    {
        _dayData = dayData;
        _viewModel = viewModel;
        Date = dayData.Date;
        Day = dayData.Date.Day;

        // 监听 CalendarDayData 的 Todos 和 Events 变化
        _dayData.Todos.CollectionChanged += OnDataCollectionChanged;
        _dayData.Events.CollectionChanged += OnDataCollectionChanged;

        // 初始化 Items
        UpdateItems();
    }

    public DateOnly Date { get; }
    public int Day { get; }

    [ObservableProperty]
    private bool _isCurrentMonth;

    [ObservableProperty]
    private bool _isToday;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private List<CalendarDayItem> _items = [];

    /// <summary>
    /// 当数据集合变化时，更新显示的 Items
    /// </summary>
    private void OnDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateItems();
    }

    /// <summary>
    /// 从 CalendarDayData 生成显示用的 Items
    /// </summary>
    private void UpdateItems()
    {
        Items = CalendarItemsHelper.GenerateMonthViewItems(
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
/// 日历日期事项（用于月视图显示）
/// </summary>
public partial class CalendarDayItem : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isTodo;

    [ObservableProperty]
    private bool _isEvent;

    [ObservableProperty]
    private bool _isMore;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private int _remainingCount;
}
