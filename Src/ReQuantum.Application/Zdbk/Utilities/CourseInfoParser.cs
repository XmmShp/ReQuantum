using ReQuantum.Application.Zdbk.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReQuantum.Application.Zdbk.Utilities;

public static class CourseInfoParser
{
    private static readonly Regex WeekRangeRegex = new(@"第(\d+)(?:-(\d+))?周", RegexOptions.Compiled);
    private static readonly Regex ExamDateTimeRegex = new(@"(\d{4})年(\d{2})月(\d{2})日\((\d{2}:\d{2})-(\d{2}:\d{2})\)", RegexOptions.Compiled);

    public static ParsedCourseInfo Parse(string kcb)
    {
        var result = new ParsedCourseInfo { RawInfo = kcb };
        if (string.IsNullOrWhiteSpace(kcb))
        {
            return result;
        }

        var lines = kcb.Split(new[] { "<br>", "<br/>", "<BR>" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length >= 1)
        {
            result.CourseName = lines[0].Trim();
        }

        if (lines.Length >= 2)
        {
            ParseWeekInfo(lines[1], result);
        }

        if (lines.Length >= 3)
        {
            result.Teacher = lines[2].Trim();
        }

        if (lines.Length >= 4)
        {
            ParseLocationAndExam(lines[3], result);
        }

        return result;
    }

    private static void ParseWeekInfo(string weekInfoLine, ParsedCourseInfo result)
    {
        var weekMatch = WeekRangeRegex.Match(weekInfoLine);
        if (weekMatch.Success)
        {
            result.WeekStart = int.Parse(weekMatch.Groups[1].Value);
            result.WeekEnd = weekMatch.Groups[2].Success && !string.IsNullOrEmpty(weekMatch.Groups[2].Value)
                ? int.Parse(weekMatch.Groups[2].Value)
                : result.WeekStart;
        }
    }

    private static void ParseLocationAndExam(string locationLine, ParsedCourseInfo result)
    {
        var parts = locationLine.Split(new[] { "zwf" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1)
        {
            result.Location = parts[0].Trim();
        }

        if (parts.Length >= 2)
        {
            var examMatch = ExamDateTimeRegex.Match(parts[1]);
            if (examMatch.Success)
            {
                try
                {
                    int year = int.Parse(examMatch.Groups[1].Value);
                    int month = int.Parse(examMatch.Groups[2].Value);
                    int day = int.Parse(examMatch.Groups[3].Value);
                    result.ExamDate = new DateTime(year, month, day);
                    result.ExamStartTime = TimeOnly.Parse(examMatch.Groups[4].Value, CultureInfo.InvariantCulture);
                    result.ExamEndTime = TimeOnly.Parse(examMatch.Groups[5].Value, CultureInfo.InvariantCulture);
                }
                catch { /* ignore parse errors */ }
            }
        }
    }
}
