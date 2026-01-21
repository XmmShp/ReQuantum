using ReQuantum.Modules.Zdbk.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReQuantum.Modules.Zdbk.Parsers;

/// <summary>
/// 课程信息解析器，用于解析教务网返回的 kcb 字段（HTML 格式）
/// </summary>
public static class CourseInfoParser
{
    // 正则表达式：匹配周次范围 "第X-Y周" 或 "第X周"
    private static readonly Regex WeekRangeRegex = new(@"第(\d+)(?:-(\d+))?周", RegexOptions.Compiled);

    // 正则表达式：匹配考试日期时间 "YYYY年MM月DD日(HH:MM-HH:MM)"
    private static readonly Regex ExamDateTimeRegex = new(@"(\d{4})年(\d{2})月(\d{2})日\((\d{2}:\d{2})-(\d{2}:\d{2})\)", RegexOptions.Compiled);

    /// <summary>
    /// 解析 kcb 字段中的课程信息
    /// </summary>
    /// <param name="kcb">原始课程信息字符串
    /// </param>
    /// <returns>解析后的课程信息</returns>
    public static ParsedCourseInfo Parse(string kcb)
    {
        var result = new ParsedCourseInfo
        {
            RawInfo = kcb
        };

        if (string.IsNullOrWhiteSpace(kcb))
        {
            return result;
        }

        // 按 <br> 分割行
        var lines = kcb.Split(new[] { "<br>", "<br/>", "<BR>" }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length >= 1)
        {
            // 第1行：课程名称
            result.CourseName = lines[0].Trim();
        }

        if (lines.Length >= 2)
        {
            // 第2行：周次信息，格式如 "秋冬{第1-8周|3节/单周}"
            ParseWeekInfo(lines[1], result);
        }

        if (lines.Length >= 3)
        {
            // 第3行：教师姓名
            result.Teacher = lines[2].Trim();
        }

        if (lines.Length >= 4)
        {
            // 第4行：教室 + 考试时间（如果有）
            ParseLocationAndExam(lines[3], result);
        }

        return result;
    }

    /// <summary>
    /// 解析周次信息
    /// </summary>
    private static void ParseWeekInfo(string weekInfoLine, ParsedCourseInfo result)
    {
        // 正则匹配：第X-Y周 或 第X周
        var weekMatch = WeekRangeRegex.Match(weekInfoLine);
        if (weekMatch.Success)
        {
            result.WeekStart = int.Parse(weekMatch.Groups[1].Value);

            if (weekMatch.Groups[2].Success && !string.IsNullOrEmpty(weekMatch.Groups[2].Value))
            {
                result.WeekEnd = int.Parse(weekMatch.Groups[2].Value);
            }
            else
            {
                // 只有单周，如"第5周"
                result.WeekEnd = result.WeekStart;
            }
        }
    }

    /// <summary>
    /// 解析教室和考试时间
    /// </summary>
    private static void ParseLocationAndExam(string locationLine, ParsedCourseInfo result)
    {
        //第一部分是教室，后面可能是考试时间
        var parts = locationLine.Split(new[] { "zwf" }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 1)
        {
            result.Location = parts[0].Trim();
        }

        // 尝试解析考试时间
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

                    // 解析时间
                    var startTime = TimeOnly.Parse(examMatch.Groups[4].Value, CultureInfo.InvariantCulture);
                    var endTime = TimeOnly.Parse(examMatch.Groups[5].Value, CultureInfo.InvariantCulture);

                    result.ExamStartTime = startTime;
                    result.ExamEndTime = endTime;
                }
                catch
                {
                    // 解析失败，忽略考试时间
                }
            }
        }
    }
}