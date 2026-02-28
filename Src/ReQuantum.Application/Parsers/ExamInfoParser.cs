using ReQuantum.Application.Models.Zdbk;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReQuantum.Application.Parsers;

public static partial class ExamTimeParser
{
    [GeneratedRegex(@"(\d{4})年(\d{2})月(\d{2})日\((\d{2}:\d{2})-(\d{2}:\d{2})\)")]
    private static partial Regex OldFormatRegex();

    [GeneratedRegex(@"([秋冬春夏])考试第(\d+)天\((\d{2}:\d{2})-(\d{2}:\d{2})\)")]
    private static partial Regex NewFormatRegex();

    public static (DateTime? Start, DateTime? End) Parse(string? timeString, AcademicCalendar? calendar = null)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return (null, null);
        }

        var oldMatch = OldFormatRegex().Match(timeString);
        if (oldMatch.Success)
        {
            return ParseOldFormat(oldMatch);
        }

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
        catch { return (null, null); }
    }

    private static (DateTime?, DateTime?) ParseNewFormat(Match match, AcademicCalendar? calendar)
    {
        if (calendar == null)
        {
            return (null, null);
        }

        try
        {
            // TODO: calculate exam date from calendar
            return (null, null);
        }
        catch { return (null, null); }
    }
}
