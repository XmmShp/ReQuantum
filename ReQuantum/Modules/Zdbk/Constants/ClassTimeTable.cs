using System;
using System.Collections.Generic;

namespace ReQuantum.Modules.Zdbk.Constants;

/// <summary>
/// 节次时间表 (基于浙江大学标准作息时间)
/// </summary>
public static class ClassTimeTable
{
    /// <summary>
    /// 节次时间映射 (节次 -> (开始时间, 结束时间))
    /// </summary>
    public static readonly Dictionary<int, (TimeOnly Start, TimeOnly End)> SectionTimeMap = new()
    {
        { 1, (new TimeOnly(8, 0), new TimeOnly(8, 45)) },    // 第1节 08:00-08:45
        { 2, (new TimeOnly(8, 50), new TimeOnly(9, 35)) },   // 第2节 08:50-09:35
        { 3, (new TimeOnly(10, 0), new TimeOnly(10, 45)) },  // 第3节 10:00-10:45
        { 4, (new TimeOnly(10, 50), new TimeOnly(11, 35)) }, // 第4节 10:50-11:35
        { 5, (new TimeOnly(11, 40), new TimeOnly(12, 25)) }, // 第5节 11:40-12:25
        { 6, (new TimeOnly(13, 25), new TimeOnly(14, 10)) }, // 第6节 13:25-14:10
        { 7, (new TimeOnly(14, 15), new TimeOnly(15, 0)) },  // 第7节 14:15-15:00
        { 8, (new TimeOnly(15, 5), new TimeOnly(15, 50)) },  // 第8节 15:05-15:50
        { 9, (new TimeOnly(16, 15), new TimeOnly(17, 00)) }, // 第9节 16:10-16:55
        { 10, (new TimeOnly(17, 5), new TimeOnly(17, 50)) }, // 第10节 17:00-17:45
        { 11, (new TimeOnly(18, 50), new TimeOnly(19, 35)) }, // 第11节 18:50-19:35
        { 12, (new TimeOnly(19, 40), new TimeOnly(20, 25)) },  // 第12节 19:40-20:25
        { 13, (new TimeOnly(20, 30), new TimeOnly(21, 15)) }  // 第13节 20:30-21:15
    };

    /// <summary>
    /// 根据起始节次和持续长度计算上课时间范围
    /// </summary>
    /// <param name="startSection">起始节次 (1-13)</param>
    /// <param name="duration">持续节数 (1-5)</param>
    /// <returns>开始时间和结束时间</returns>
    public static (TimeOnly Start, TimeOnly End) GetClassTime(int startSection, int duration)
    {
        if (!SectionTimeMap.TryGetValue(startSection, out var startTime))
        {
            throw new ArgumentException($"Invalid start section: {startSection}");
        }

        var endSection = startSection + duration - 1;
        if (!SectionTimeMap.TryGetValue(endSection, out var endTime))
        {
            throw new ArgumentException($"Invalid end section: {endSection}");
        }

        return (startTime.Start, endTime.End);
    }
}