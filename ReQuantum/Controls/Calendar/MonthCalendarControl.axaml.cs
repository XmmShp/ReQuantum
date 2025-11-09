using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReQuantum.Controls;

public class MonthCalendarControl : TemplatedControl
{
    public static readonly StyledProperty<int> YearProperty =
        AvaloniaProperty.Register<MonthCalendarControl, int>(nameof(Year), DateTime.Now.Year);

    public static readonly StyledProperty<int> MonthProperty =
        AvaloniaProperty.Register<MonthCalendarControl, int>(nameof(Month), DateTime.Now.Month);

    public static readonly StyledProperty<DateOnly> SelectedDateProperty =
        AvaloniaProperty.Register<MonthCalendarControl, DateOnly>(nameof(SelectedDate), DateOnly.FromDateTime(DateTime.Now));

    public static readonly DirectProperty<MonthCalendarControl, List<CalendarDay>> CalendarDaysProperty =
        AvaloniaProperty.RegisterDirect<MonthCalendarControl, List<CalendarDay>>(
            nameof(CalendarDays),
            o => o.CalendarDays);

    public int Year
    {
        get => GetValue(YearProperty);
        set => SetValue(YearProperty, value);
    }

    public int Month
    {
        get => GetValue(MonthProperty);
        set => SetValue(MonthProperty, value);
    }

    public DateOnly SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public event EventHandler<DateOnly>? DateSelected;

    static MonthCalendarControl()
    {
        YearProperty.Changed.AddClassHandler<MonthCalendarControl>((x, e) => x.UpdateCalendar());
        MonthProperty.Changed.AddClassHandler<MonthCalendarControl>((x, e) => x.UpdateCalendar());
        SelectedDateProperty.Changed.AddClassHandler<MonthCalendarControl>((x, e) => x.UpdateCalendar());
    }

    public MonthCalendarControl()
    {
        // 初始化时生成日历数据
        UpdateCalendar();
    }

    private List<CalendarDay> _calendarDays = [];
    private CalendarDay? _previousSelectedDay;

    public List<CalendarDay> CalendarDays
    {
        get => _calendarDays;
        private set => SetAndRaise(CalendarDaysProperty, ref _calendarDays, value);
    }

    private void UpdateCalendar()
    {
        CalendarDays = GenerateCalendarDays(Year, Month);
        // 重建日历后需要重新查找选中的日期
        _previousSelectedDay = CalendarDays.FirstOrDefault(d => d.IsSelected);
    }

    public void SelectDate(DateOnly date)
    {
        SelectedDate = date;
        DateSelected?.Invoke(this, date);
        // 只更新选中状态，不重建整个日历
        UpdateSelectionState(date);
    }

    /// <summary>
    /// 更新指定日期的事项列表
    /// </summary>
    public void UpdateDayItems(DateOnly date, List<CalendarDayItem> items)
    {
        var day = CalendarDays.FirstOrDefault(d => d.Date == date);
        if (day != null)
        {
            day.Items = items;
        }
    }

    /// <summary>
    /// 批量更新所有日期的事项
    /// </summary>
    public void UpdateAllDayItems(Dictionary<DateOnly, List<CalendarDayItem>> itemsDict)
    {
        foreach (var day in CalendarDays)
        {
            if (itemsDict.TryGetValue(day.Date, out var items))
            {
                day.Items = items;
            }
            else
            {
                day.Items = [];
            }
        }
    }

    private void UpdateSelectionState(DateOnly newSelectedDate)
    {
        // 取消上一个选中的日期
        if (_previousSelectedDay != null)
        {
            _previousSelectedDay.IsSelected = false;
        }

        // 选中新日期
        var newSelectedDay = CalendarDays.FirstOrDefault(d => d.Date == newSelectedDate);
        if (newSelectedDay != null)
        {
            newSelectedDay.IsSelected = true;
            _previousSelectedDay = newSelectedDay;
        }
    }

    private List<CalendarDay> GenerateCalendarDays(int year, int month)
    {
        var days = new List<CalendarDay>();
        var firstDay = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var selectedDate = SelectedDate;

        // 获取本月第一天是星期几（0=Sunday, 1=Monday, ...）
        var firstDayOfWeek = (int)firstDay.DayOfWeek;

        // 添加上个月的日期（填充前面的空白）
        if (firstDayOfWeek > 0)
        {
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;
            var daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

            for (var i = firstDayOfWeek - 1; i >= 0; i--)
            {
                var day = daysInPrevMonth - i;
                var date = new DateOnly(prevYear, prevMonth, day);
                days.Add(new CalendarDay
                {
                    Date = date,
                    Day = day,
                    IsCurrentMonth = false,
                    IsToday = false,
                    IsSelected = date == selectedDate,
                    ParentControl = this
                });
            }
        }

        // 添加本月的日期
        var today = DateOnly.FromDateTime(DateTime.Now);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            days.Add(new CalendarDay
            {
                Date = date,
                Day = day,
                IsCurrentMonth = true,
                IsToday = date == today,
                IsSelected = date == selectedDate,
                ParentControl = this
            });
        }

        // 添加下个月的日期（填充后面的空白，确保总共6周42天）
        var remainingDays = 42 - days.Count;
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;

        for (var day = 1; day <= remainingDays; day++)
        {
            var date = new DateOnly(nextYear, nextMonth, day);
            days.Add(new CalendarDay
            {
                Date = date,
                Day = day,
                IsCurrentMonth = false,
                IsToday = false,
                IsSelected = date == selectedDate,
                ParentControl = this
            });
        }

        return days;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateCalendar();
    }
}

public partial class CalendarDay : ObservableObject
{
    public DateOnly Date { get; set; }
    public int Day { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private List<CalendarDayItem> _items = [];

    public MonthCalendarControl? ParentControl { get; set; }

    private RelayCommand? _selectCommand;
    public RelayCommand Select => _selectCommand ??= new RelayCommand(() => ParentControl?.SelectDate(Date));
}

/// <summary>
/// 日历日期事项（用于月视图显示）
/// </summary>
public partial class CalendarDayItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

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
