using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReQuantum.Modules.Pta.Services;

public interface IPtaCalendarConvertService
{
    List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets);
}

[AutoInject(Lifetime.Singleton)]
public class PtaCalendarConvertService : IPtaCalendarConvertService
{
    public List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets)
    {
        // 转换 30 天内的所有习题集（包括已过期但在 30 天内的）
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        return problemSets
            .Where(ps => ps.EndAt > thirtyDaysAgo)
            .Select(ps => new CalendarEvent
            {
                Content = $"{ps.Name}的DDL",
                // 显示在截止日期当天，设置为接近截止时间
                StartTime = ps.EndAt,
                EndTime = ps.EndAt,
                IsFromPta = true,
                From = "PTA"
            })
            .ToList();
    }
}
