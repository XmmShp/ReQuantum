using System;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.CoursesZju.Models;

public class CoursesZjuTodoDto
{
    [JsonPropertyName("id")]
    public required long Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// 结束时间(UTC)
    /// </summary>
    [JsonPropertyName("end_time")]
    public required DateTime EndTime { get; init; }

    [JsonPropertyName("course_name")]
    public required string CourseName { get; init; }

    [JsonPropertyName("course_type")]
    public required int CourseType { get; init; }

    [JsonPropertyName("course_id")]
    public required int CourseId { get; init; }

    [JsonPropertyName("course_code")]
    public required string CourseCode { get; init; }

    [JsonPropertyName("is_student")]
    public required bool IsStudent { get; init; }

    [JsonPropertyName("is_locked")]
    public required bool IsLocked { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is CoursesZjuTodoDto item)
        {
            return item.Id == Id;
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
