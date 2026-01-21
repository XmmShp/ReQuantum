using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ReQuantum.Modules.Zdbk.Models;

namespace ReQuantum.Modules.Zdbk.Parsers;

/// <summary>
/// 考试时间解析器，基于ZDBK上的考试时间格式
/// </summary>
public static partial class ExamTimeParser
{
    // 旧格式示例: "2024年06月28日(08:00-10:00)"
    [GeneratedRegex(@"(\d{4})年(\d{2})月(\d{2})日\((\d{2}:\d{2})-(\d{2}:\d{2})\)")]
    private static partial Regex OldFormatRegex();

    // 新格式示例: "冬考试第2天(10:30-12:30)"
    [GeneratedRegex(@"([秋冬春夏])考试第(\d+)天\((\d{2}:\d{2})-(\d{2}:\d{2})\)")]
    private static partial Regex NewFormatRegex();

    /// <summary>
    /// 解析考试时间字符串
    /// </summary>
    /// <param name="timeString">时间字符串</param>
    /// <param name="calendar">校历（用于新格式计算）</param>
    /// <returns>开始和结束时间，如果解析失败返回null</returns>
    public static (DateTime? Start, DateTime? End) Parse(string? timeString, AcademicCalendar? calendar = null)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return (null, null);
        }

        // 尝试旧格式
        var oldMatch = OldFormatRegex().Match(timeString);
        if (oldMatch.Success)
        {
            return ParseOldFormat(oldMatch);
        }

        // 尝试新格式
        var newMatch = NewFormatRegex().Match(timeString);
        if (newMatch.Success)
        {
            return ParseNewFormat(newMatch, calendar);
        }

        return (null, null);
    }

    private static (DateTime?, DateTime?) ParseOldFormat(Match match)
    {
        try
        {
            int year = int.Parse(match.Groups[1].Value);
            int month = int.Parse(match.Groups[2].Value);
            int day = int.Parse(match.Groups[3].Value);
            var date = new DateOnly(year, month, day);

            var startTime = TimeOnly.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var endTime = TimeOnly.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);

            return (date.ToDateTime(startTime), date.ToDateTime(endTime));
        }
        catch
        {
            return (null, null);
        }
    }

    private static (DateTime?, DateTime?) ParseNewFormat(Match match, AcademicCalendar? calendar)
    {
        if (calendar == null)
        {
            // TODO: 没有校历时无法计算，返回null或者记录日志
            return (null, null);
        }

        try
        {
            string semester = match.Groups[1].Value; // 秋/冬/春/夏
            int examDay = int.Parse(match.Groups[2].Value);
            var startTime = TimeOnly.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var endTime = TimeOnly.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

            // TODO: 根据校历和学期计算考试周的起始日期
            // 例如：冬学期考试周 = 学期结束日期 - 考试周数量
            // 这需要校历中包含考试周的信息

            // 临时方案：返回null，等待校历发布
            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }
}