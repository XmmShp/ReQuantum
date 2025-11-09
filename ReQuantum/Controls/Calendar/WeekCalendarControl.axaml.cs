using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReQuantum.Controls;

/// <summary>
/// 周视图日历控件
/// </summary>
public class WeekCalendarControl : TemplatedControl
{
    public static readonly StyledProperty<DateOnly> WeekStartDateProperty =
        AvaloniaProperty.Register<WeekCalendarControl, DateOnly>(nameof(WeekStartDate), DateOnly.FromDateTime(DateTime.Now));

    public static readonly StyledProperty<DateOnly> SelectedDateProperty =
        AvaloniaProperty.Register<WeekCalendarControl, DateOnly>(nameof(SelectedDate), DateOnly.FromDateTime(DateTime.Now));

    public static readonly DirectProperty<WeekCalendarControl, List<WeekDay>> WeekDaysProperty =
        AvaloniaProperty.RegisterDirect<WeekCalendarControl, List<WeekDay>>(
            nameof(WeekDays),
            o => o.WeekDays);

    public DateOnly WeekStartDate
    {
        get => GetValue(WeekStartDateProperty);
        set => SetValue(WeekStartDateProperty, value);
    }

    public DateOnly SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public event EventHandler<DateOnly>? DateSelected;

    private List<WeekDay> _weekDays = [];

    public List<WeekDay> WeekDays
    {
        get => _weekDays;
        private set => SetAndRaise(WeekDaysProperty, ref _weekDays, value);
    }

    static WeekCalendarControl()
    {
        WeekStartDateProperty.Changed.AddClassHandler<WeekCalendarControl>((x, e) => x.UpdateWeek());
        SelectedDateProperty.Changed.AddClassHandler<WeekCalendarControl>((x, e) => x.UpdateWeek());
    }

    public WeekCalendarControl()
    {
        // 初始化时生成周数据
        UpdateWeek();
    }

    private WeekDay? _previousSelectedDay;

    private void UpdateWeek()
    {
        WeekDays = GenerateWeekDays(WeekStartDate);
        // 重建周数据后需要重新查找选中的日期
        _previousSelectedDay = WeekDays.FirstOrDefault(d => d.IsSelected);
    }

    public void SelectDate(DateOnly date)
    {
        SelectedDate = date;
        DateSelected?.Invoke(this, date);
        // 只更新选中状态，不重建整个列表
        UpdateSelectionState(date);
    }

    private void UpdateSelectionState(DateOnly newSelectedDate)
    {
        // 取消上一个选中的日期
        if (_previousSelectedDay != null)
        {
            _previousSelectedDay.IsSelected = false;
        }

        // 选中新日期
        var newSelectedDay = WeekDays.FirstOrDefault(d => d.Date == newSelectedDate);
        if (newSelectedDay != null)
        {
            newSelectedDay.IsSelected = true;
            _previousSelectedDay = newSelectedDay;
        }
    }

    /// <summary>
    /// 更新指定日期的时间线事项
    /// </summary>
    public void UpdateDayItems(DateOnly date, List<WeekTimelineItem> items)
    {
        var day = WeekDays.FirstOrDefault(d => d.Date == date);
        if (day != null)
        {
            day.TimelineItems = items;
        }
    }

    /// <summary>
    /// 批量更新所有日期的时间线事项
    /// </summary>
    public void UpdateAllDayItems(Dictionary<DateOnly, List<WeekTimelineItem>> itemsDict)
    {
        foreach (var day in WeekDays)
        {
            if (itemsDict.TryGetValue(day.Date, out var items))
            {
                day.TimelineItems = items;
            }
            else
            {
                day.TimelineItems = [];
            }
        }
    }

    private List<WeekDay> GenerateWeekDays(DateOnly weekStartDate)
    {
        var days = new List<WeekDay>();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var selectedDate = SelectedDate;

        for (var i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            days.Add(new WeekDay
            {
                Date = date,
                Day = date.Day,
                DayOfWeek = date.DayOfWeek.ToString()[..3],
                IsToday = date == today,
                IsSelected = date == selectedDate,
                ParentControl = this
            });
        }

        return days;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateWeek();
    }
}

public partial class WeekDay : ObservableObject
{
    public DateOnly Date { get; set; }
    public int Day { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public bool IsToday { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private List<WeekTimelineItem> _timelineItems = [];

    public WeekCalendarControl? ParentControl { get; set; }

    private RelayCommand? _selectCommand;
    public RelayCommand Select => _selectCommand ??= new RelayCommand(() => ParentControl?.SelectDate(Date));
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
    private string _title = string.Empty;

    [ObservableProperty]
    private double _topPosition; // 在时间线上的位置（0-24小时）

    [ObservableProperty]
    private double _height; // 高度（小时数）

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private string _timeLabel = string.Empty; // 时间标注（例如："09:00 - 10:30"）
}
