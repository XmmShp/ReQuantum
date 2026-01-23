using System;
using System.Collections.Generic;
using System.Linq;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.ViewModels;

namespace ReQuantum.Modules.Calendar.Presentations;

/// <summary>
/// 日历事项辅助类
/// </summary>
public static class CalendarItemsHelper
{
    private static readonly ILocalizer Localizer;
    static CalendarItemsHelper()
    {
        Localizer = SingletonManager.Instance.GetInstance<ILocalizer>();
    }
    /// <summary>
    /// 为周视图生成时间线事项
    /// 待办显示规则：只在截止日期当天显示（在时间线中渲染）
    /// 日程：在开始到结束的所有日期显示
    /// </summary>
    public static List<TimelineItem> GenerateTimelineItems(
        DateOnly date,
        List<CalendarTodo> todos,
        List<CalendarEvent> events)
    {
        // 待办：只在截止日期当天显示
        var items = todos.Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .Select(todo => new TimelineItem
            {
                Type = TimelineItemType.Todo,
                StartTime = todo.DueTime, // 显示在截止时间位置
                Content = todo.Content,
                IsCompleted = todo.IsCompleted
            }).ToList();

        items.AddRange(events
            .Where(evt => evt.StartTime.Date <= date.ToDateTime(TimeOnly.MinValue) &&
                          evt.EndTime.Date >= date.ToDateTime(TimeOnly.MinValue))
            .Select(evt => new
            {
                evt,
                startTime =
                    evt.StartTime.Date == date.ToDateTime(TimeOnly.MinValue)
                        ? evt.StartTime
                        : date.ToDateTime(TimeOnly.MinValue)
            })
            .Select(evtStartTimePair => new
            {
                evtStartTimePair,
                endTime =
                    evtStartTimePair.evt.EndTime.Date == date.ToDateTime(TimeOnly.MinValue)
                        ? evtStartTimePair.evt.EndTime
                        : date.ToDateTime(new TimeOnly(23, 59, 59))
            })
            .Select(evtStartTimeEndTimePair => new TimelineItem
            {
                Type = TimelineItemType.Event,
                StartTime = evtStartTimeEndTimePair.evtStartTimePair.startTime,
                EndTime = evtStartTimeEndTimePair.endTime,
                Content = evtStartTimeEndTimePair.evtStartTimePair.evt.Content
            }));

        return items.OrderBy(i => i.StartTime).ToList();
    }

    /// <summary>
    /// 为周视图生成 WeekTimelineItem 列表
    /// </summary>
    public static List<WeekTimelineItem> GenerateWeekTimelineItems(
        DateOnly date,
        List<CalendarTodo> todos,
        List<CalendarEvent> events)
    {
        var timelineItems = GenerateTimelineItems(date, todos, events);

        return timelineItems.Select(item => new WeekTimelineItem
        {
            IsTodo = item.Type == TimelineItemType.Todo,
            IsEvent = item.Type == TimelineItemType.Event,
            Content = item.Content,
            TopPosition = item.TopPosition,
            Height = item.Height,
            IsCompleted = item.IsCompleted,
            TimeLabel = GenerateTimeLabel(item)
        }).ToList();
    }

    /// <summary>
    /// 生成时间标注文本
    /// </summary>
    private static string GenerateTimeLabel(TimelineItem item)
    {
        if (item.Type == TimelineItemType.Todo)
        {
            // 待办只显示截止时间
            return item.StartTime.ToString("HH:mm");
        }

        // 日程显示开始和结束时间
        if (item.EndTime.HasValue)
        {
            return $"{item.StartTime:HH:mm} - {item.EndTime.Value:HH:mm}";
        }
        return item.StartTime.ToString("HH:mm");
    }

    /// <summary>
    /// 为月视图生成简化事项列表（基于待办和日程列表）
    /// </summary>
    public static List<CalendarDayItem> GenerateMonthViewItems(
        List<CalendarTodo> todos,
        List<CalendarEvent> events)
    {
        var items = todos.Select(todo => new CalendarDayItem
        {
            IsTodo = true,
            Content = todo.Content,
            IsCompleted = todo.IsCompleted
        }).ToList();

        items.AddRange(events.Select(evt => new CalendarDayItem
        {
            IsEvent = true,
            Content = evt.Content
        }));

        if (items.Count <= 3)
        {
            return items;
        }

        var result = items.Take(2).ToList();
        result.Add(new CalendarDayItem
        {
            IsMore = true,
            Content = Localizer[nameof(UIText.MoreItems), items.Count - 2],
            RemainingCount = items.Count - 2
        });
        return result;
    }

    /// <summary>
    /// 为周视图生成时间线事项列表（基于待办和日程列表）
    /// </summary>
    public static List<WeekTimelineItem> GenerateWeekViewItems(
        List<CalendarTodo> todos,
        List<CalendarEvent> events)
    {
        if (todos.Count == 0 && events.Count == 0)
        {
            return [];
        }

        // 使用第一个事项的日期（因为传入的都是同一天的）
        var date = todos.Count > 0
            ? DateOnly.FromDateTime(todos[0].DueTime)
            : DateOnly.FromDateTime(events[0].StartTime);

        return GenerateWeekTimelineItems(date, todos, events);
    }

    /// <summary>
    /// 为月视图生成简化事项列表（最多3条）
    /// 待办显示规则：只在截止日期当天显示TAG
    /// </summary>
    public static List<CalendarDayItem> GenerateMonthDayItems(
        DateOnly date,
        List<CalendarTodo> todos,
        List<CalendarEvent> events)
    {
        // 待办：只在截止日期当天显示
        var items = todos.Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .Select(todo => new CalendarDayItem
            {
                IsTodo = true,
                Content = todo.Content,
                IsCompleted = todo.IsCompleted
            }).ToList();

        items.AddRange(events.Where(e =>
            e.StartTime.Date <= date.ToDateTime(TimeOnly.MinValue)
            && e.EndTime.Date >= date.ToDateTime(TimeOnly.MinValue)).Select(
            evt => new CalendarDayItem
            {
                IsEvent = true,
                Content = evt.Content
            }));

        if (items.Count <= 3)
        {
            return items;
        }

        var result = items.Take(2).ToList();
        result.Add(new CalendarDayItem
        {
            IsMore = true,
            Content = Localizer[nameof(UIText.MoreItems), items.Count - 2],
            RemainingCount = items.Count - 2
        });
        return result;

    }
}

/// <summary>
/// 时间线事项类型
/// </summary>
public enum TimelineItemType
{
    Todo,   // 待办（红线）
    Event   // 日程（蓝色区块）
}

/// <summary>
/// 时间线事项
/// </summary>
public class TimelineItem
{
    public TimelineItemType Type { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    // 计算在时间线上的位置和高度（0-24小时）
    public double TopPosition => StartTime.Hour + StartTime.Minute / 60.0;
    public double Height => EndTime.HasValue
        ? (EndTime.Value - StartTime).TotalHours
        : 1; // 待办显示为1小时高度的区块
}
