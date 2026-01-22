using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Modules.Calendar.Entities;

namespace ReQuantum.Models;

/// <summary>
/// 日历日期数据模型（纯数据，不包含UI状态）
/// </summary>
public partial class CalendarDayData : ObservableObject
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 该日期的待办事项
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CalendarTodo> _todos = [];

    /// <summary>
    /// 该日期的日程事件
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CalendarEvent> _events = [];
}
