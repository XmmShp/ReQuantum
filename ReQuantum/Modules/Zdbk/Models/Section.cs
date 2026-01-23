using System;
using System.Collections.Generic;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 表示某门课程的教学班(Id为选课课号)
/// </summary>
public class Section
{
    public required string Id { get; init; }
    public required Course Course { get; init; }
    public HashSet<string> Instructors { get; set; } = [];
    public string Semesters { get; set; } = string.Empty;
    public HashSet<(string Schedule, string Location)> ScheduleAndLocations { get; set; } = [];
    public string TeachingForm { get; set; } = string.Empty;
    public string TeachingMethod { get; set; } = string.Empty;
    public TimeSlot? ExamTime { get; set; }
    public bool IsInternational { get; set; }

    public virtual SectionSnapshot CreateSnapshot() => new()
    {
        CourseCredits = Course.Credits,
        CourseId = Course.Id,
        CourseName = Course.Name,
        Id = Id,
        Instructors = Instructors,
        ScheduleAndLocations = ScheduleAndLocations,
        ExamTime = ExamTime,
        Semesters = Semesters,
        IsInternational = IsInternational,
        TeachingForm = TeachingForm,
        TeachingMethod = TeachingMethod
    };

    public override bool Equals(object? obj)
    {
        if (obj is not Section section)
        {
            return false;
        }
        return Id == section.Id;
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
