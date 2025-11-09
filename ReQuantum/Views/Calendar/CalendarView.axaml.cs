using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ReQuantum.Controls;
using ReQuantum.ViewModels;
using System;
using System.Collections.Generic;

namespace ReQuantum.Views;

public partial class CalendarView : UserControl
{

    public CalendarView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 查找所有日历控件并绑定事件
        BindCalendarEvents(this);

        // 初始加载数据
        if (DataContext is CalendarViewModel viewModel)
        {
            UpdateCalendarData(viewModel.CalendarPartViewModel);
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CalendarViewModel viewModel)
        {
            viewModel.CalendarPartViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (DataContext is not CalendarViewModel viewModel)
            return;

        var calendarVm = viewModel.CalendarPartViewModel;

        // 当视图类型变化时，重新绑定控件（因为控件可能刚刚被创建）
        if (e.PropertyName == nameof(CalendarPartViewModel.CurrentViewType))
        {
            // 延迟执行，确保控件已经渲染
            Dispatcher.UIThread.Post(() =>
            {
                BindCalendarEvents(this);
                UpdateCalendarData(calendarVm);
            }, DispatcherPriority.Loaded);
        }
        // 当年月、选中日期、周起始日期变化时，更新日历控件的数据
        else if (e.PropertyName == nameof(CalendarPartViewModel.SelectedYear) ||
            e.PropertyName == nameof(CalendarPartViewModel.SelectedMonth) ||
            e.PropertyName == nameof(CalendarPartViewModel.SelectedDate) ||
            e.PropertyName == nameof(CalendarPartViewModel.WeekStartDate))
        {
            UpdateCalendarData(calendarVm);
        }
    }

    private readonly List<MonthCalendarControl> _monthCalendarControls = [];
    private readonly List<WeekCalendarControl> _weekCalendarControls = [];

    private void BindCalendarEvents(ILogical element)
    {
        foreach (var child in element.LogicalChildren)
        {
            switch (child)
            {
                case MonthCalendarControl monthControl:
                    {
                        if (!_monthCalendarControls.Contains(monthControl))
                        {
                            _monthCalendarControls.Add(monthControl);
                            monthControl.DateSelected -= OnDateSelected;
                            monthControl.DateSelected += OnDateSelected;
                        }

                        break;
                    }
                case WeekCalendarControl weekControl:
                    {
                        if (!_weekCalendarControls.Contains(weekControl))
                        {
                            _weekCalendarControls.Add(weekControl);
                            weekControl.DateSelected -= OnDateSelected;
                            weekControl.DateSelected += OnDateSelected;
                        }

                        break;
                    }
            }

            BindCalendarEvents(child);
        }
    }

    private void OnDateSelected(object? sender, DateOnly date)
    {
        if (DataContext is CalendarViewModel viewModel)
        {
            viewModel.CalendarPartViewModel.SelectDateCommand.Execute(date);
        }
    }

    private void UpdateCalendarData(CalendarPartViewModel viewModel)
    {
        // 获取当前月份的所有待办和日程
        var startDate = new DateOnly(viewModel.SelectedYear, viewModel.SelectedMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var todos = viewModel.GetTodosInRange(startDate, endDate);
        var events = viewModel.GetEventsInRange(startDate, endDate);

        // 更新所有月视图控件（桌面端和移动端）
        foreach (var monthControl in _monthCalendarControls)
        {
            var monthItems = new Dictionary<DateOnly, List<CalendarDayItem>>();

            for (var date = startDate.AddDays(-7); date <= endDate.AddDays(7); date = date.AddDays(1))
            {
                var items = CalendarItemsHelper.GenerateMonthDayItems(date, todos, events);
                monthItems[date] = items;
            }

            monthControl.UpdateAllDayItems(monthItems);
        }

        // 更新所有周视图控件
        foreach (var weekControl in _weekCalendarControls)
        {
            var weekStartDate = weekControl.WeekStartDate;
            var weekEndDate = weekStartDate.AddDays(6);

            // 获取整周的待办和日程
            var weekTodos = viewModel.GetTodosInRange(weekStartDate, weekEndDate);
            var weekEvents = viewModel.GetEventsInRange(weekStartDate, weekEndDate);

            var weekItems = new Dictionary<DateOnly, List<WeekTimelineItem>>();

            // 为每一天生成时间线事项
            for (var date = weekStartDate; date <= weekEndDate; date = date.AddDays(1))
            {
                var items = CalendarItemsHelper.GenerateWeekTimelineItems(date, weekTodos, weekEvents);
                weekItems[date] = items;
            }

            weekControl.UpdateAllDayItems(weekItems);
        }
    }
}
