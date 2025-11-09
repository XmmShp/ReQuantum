using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Attributes;
using ReQuantum.Infrastructure;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Collections.Generic;

namespace ReQuantum.ViewModels;

/// <summary>
/// 日历视图类型
/// </summary>
public enum CalendarViewType
{
    Week,   // 周视图
    Month   // 月视图
}

[AutoInject(Lifetime.Transient, RegisterTypes = [typeof(CalendarPartViewModel)])]
public partial class CalendarPartViewModel : ViewModelBase<CalendarPartView>
{
    private readonly ICalendarService _calendarService;
    
    /// <summary>
    /// 动态年月文本：2025年11月 / 2025/11
    /// </summary>
    public LocalizedText YearMonthText { get; }

    #region 视图状态

    [ObservableProperty]
    private CalendarViewType _currentViewType = CalendarViewType.Month;

    [ObservableProperty]
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);

    [ObservableProperty]
    private int _selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private int _selectedMonth = DateTime.Now.Month;

    /// <summary>
    /// 周视图的起始日期（该周的第一天，星期日）
    /// </summary>
    [ObservableProperty]
    private DateOnly _weekStartDate = GetWeekStartDate(DateOnly.FromDateTime(DateTime.Now));

    #endregion

    public CalendarPartViewModel(ICalendarService calendarService)
    {
        _calendarService = calendarService;
        YearMonthText = new LocalizedText(Localizer);
        UpdateYearMonthText();
    }

    #region 视图切换

    [RelayCommand]
    private void SwitchToWeekView()
    {
        CurrentViewType = CalendarViewType.Week;
        WeekStartDate = GetWeekStartDate(SelectedDate);
    }

    [RelayCommand]
    private void SwitchToMonthView()
    {
        CurrentViewType = CalendarViewType.Month;
    }


    #endregion

    #region 日期导航

    [RelayCommand]
    private void SelectDate(DateOnly date)
    {
        SelectedDate = date;
    }

    /// <summary>
    /// 向前导航（根据当前视图类型）
    /// </summary>
    [RelayCommand]
    private void GoToPrevious()
    {
        switch (CurrentViewType)
        {
            case CalendarViewType.Month:
                if (SelectedMonth == 1)
                {
                    SelectedYear--;
                    SelectedMonth = 12;
                }
                else
                {
                    SelectedMonth--;
                }
                break;
            case CalendarViewType.Week:
                WeekStartDate = WeekStartDate.AddDays(-7);
                SelectedDate = WeekStartDate;
                break;
        }
    }

    /// <summary>
    /// 向后导航（根据当前视图类型）
    /// </summary>
    [RelayCommand]
    private void GoToNext()
    {
        switch (CurrentViewType)
        {
            case CalendarViewType.Month:
                if (SelectedMonth == 12)
                {
                    SelectedYear++;
                    SelectedMonth = 1;
                }
                else
                {
                    SelectedMonth++;
                }
                break;
            case CalendarViewType.Week:
                WeekStartDate = WeekStartDate.AddDays(7);
                SelectedDate = WeekStartDate;
                break;
        }
    }

    /// <summary>
    /// 回到今天
    /// </summary>
    [RelayCommand]
    private void GoToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        
        // 先更新年月，再更新选中日期，确保日历数据正确加载
        SelectedYear = today.Year;
        SelectedMonth = today.Month;
        WeekStartDate = GetWeekStartDate(today);
        SelectedDate = today;
        
        // 强制触发日历更新
        UpdateCalendarDays();
    }

    partial void OnSelectedYearChanged(int value)
    {
        if (value > 0)
        {
            SelectedDate = new DateOnly(value, SelectedMonth, Math.Min(SelectedDate.Day, DateTime.DaysInMonth(value, SelectedMonth)));
            UpdateYearMonthText();
            UpdateCalendarDays();
        }
    }

    partial void OnSelectedMonthChanged(int value)
    {
        if (value is >= 1 and <= 12)
        {
            SelectedDate = new DateOnly(SelectedYear, value, Math.Min(SelectedDate.Day, DateTime.DaysInMonth(SelectedYear, value)));
            UpdateYearMonthText();
            UpdateCalendarDays();
        }
    }
    
    private void UpdateYearMonthText()
    {
        YearMonthText.Set(nameof(UIText.YearMonthFormat), SelectedYear, SelectedMonth);
    }

    /// <summary>
    /// 更新日历显示的天数，并添加事件标记
    /// </summary>
    private void UpdateCalendarDays()
    {
        // 让日历控件重新生成
        OnPropertyChanged(nameof(SelectedYear));
        OnPropertyChanged(nameof(SelectedMonth));
    }

    /// <summary>
    /// 获取指定日期所在周的起始日期（星期日）
    /// </summary>
    private static DateOnly GetWeekStartDate(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        return date.AddDays(-dayOfWeek);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取指定日期范围内的所有日程（用于日历视图显示）
    /// </summary>
    public List<CalendarEvent> GetEventsInRange(DateOnly startDate, DateOnly endDate)
    {
        return _calendarService.GetEventsByDateRange(startDate, endDate);
    }

    /// <summary>
    /// 获取指定日期范围内的所有待办（用于日历视图显示）
    /// 只返回截止日期在范围内的待办
    /// </summary>
    public List<CalendarTodo> GetTodosInRange(DateOnly startDate, DateOnly endDate)
    {
        return _calendarService.GetTodosByDateRange(startDate, endDate);
    }

    #endregion
}
