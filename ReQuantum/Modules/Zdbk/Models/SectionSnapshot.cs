using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReQuantum.Modules.Zdbk.Utilities;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教学班的完整快照，包含课程信息
/// </summary>
[JsonConverter(typeof(SectionSnapshotJsonConverter))]
public record SectionSnapshot
{
    // Course 相关信息
    public string CourseName { get; init; } = string.Empty;
    public decimal CourseCredits { get; init; }
    public string CourseId { get; init; } = string.Empty;

    // Section 相关信息
    public required string Id { get; init; }
    public TimeSlot? ExamTime { get; init; }
    public IReadOnlySet<string> Instructors { get; init; } = new HashSet<string>();
    public string Semesters { get; init; } = string.Empty;
    public IReadOnlySet<(string Schedule, string Location)> ScheduleAndLocations { get; init; } = new HashSet<(string Schedule, string Location)>();
    public string TeachingForm { get; init; } = string.Empty;
    public string TeachingMethod { get; init; } = string.Empty;
    public bool IsInternational { get; init; }

    public IReadOnlyDictionary<string, string> ExtraProperties { get; init; } = new Dictionary<string, string>();

    public virtual bool Equals(SectionSnapshot? other)
        => EqualityComparer<string>.Default.Equals(Id, other?.Id);

    public override int GetHashCode() => HashCode.Combine(Id);
}