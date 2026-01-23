using System;
using System.Collections.Generic;
using ReQuantum.Modules.Zdbk.Enums;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 表示某门课程
/// </summary>
public class Course
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required decimal Credits { get; set; }
    public CourseCategory Category { get; set; }
    public string WeekTime { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public List<SelectableSection> Sections { get; init; } = [];

    public override bool Equals(object? obj)
    {
        if (obj is not Course course)
        {
            return false;
        }
        return Id == course.Id;
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}