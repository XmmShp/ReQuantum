using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReQuantum.Modules.Zdbk.Models;

namespace ReQuantum.Modules.Zdbk.Utilities;

/// <summary>
/// SectionSnapshot 的自定义 JSON 序列化器
/// </summary>
public class SectionSnapshotJsonConverter : JsonConverter<SectionSnapshot>
{
    public override SectionSnapshot Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var id = string.Empty;
        var courseName = string.Empty;
        var courseCredits = 0m;
        var courseId = string.Empty;
        TimeSlot? examTime = null;
        var instructors = new HashSet<string>();
        var semesters = string.Empty;
        var scheduleAndLocations = new HashSet<(string Schedule, string Location)>();
        var teachingForm = string.Empty;
        var teachingMethod = string.Empty;
        var isInternational = false;
        var extraProperties = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new SectionSnapshot
                {
                    Id = id,
                    CourseName = courseName,
                    CourseCredits = courseCredits,
                    CourseId = courseId,
                    ExamTime = examTime,
                    Instructors = instructors,
                    Semesters = semesters,
                    ScheduleAndLocations = scheduleAndLocations,
                    TeachingForm = teachingForm,
                    TeachingMethod = teachingMethod,
                    IsInternational = isInternational,
                    ExtraProperties = extraProperties
                };
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Id":
                    id = reader.GetString() ?? string.Empty;
                    break;
                case "CourseName":
                    courseName = reader.GetString() ?? string.Empty;
                    break;
                case "CourseCredits":
                    courseCredits = reader.GetDecimal();
                    break;
                case "CourseId":
                    courseId = reader.GetString() ?? string.Empty;
                    break;
                case "ExamTime":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        examTime = JsonSerializer.Deserialize<TimeSlot>(ref reader, options);
                    }
                    break;
                case "Instructors":
                    instructors = ReadStringArray(ref reader);
                    break;
                case "Semesters":
                    semesters = reader.GetString() ?? string.Empty;
                    break;
                case "ScheduleAndLocations":
                    scheduleAndLocations = ReadScheduleAndLocations(ref reader);
                    break;
                case "TeachingForm":
                    teachingForm = reader.GetString() ?? string.Empty;
                    break;
                case "TeachingMethod":
                    teachingMethod = reader.GetString() ?? string.Empty;
                    break;
                case "IsInternational":
                    isInternational = reader.GetBoolean();
                    break;
                case "ExtraProperties":
                    if (reader.TokenType != JsonTokenType.StartObject)
                    { continue; }

                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName)
                        { continue; }

                        var key = reader.GetString();
                        reader.Read();

                        if (key != null && reader.TokenType == JsonTokenType.String)
                        {
                            extraProperties[key] = reader.GetString() ?? string.Empty;
                        }
                    }
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, SectionSnapshot value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Id", value.Id);
        writer.WriteString("CourseName", value.CourseName);
        writer.WriteNumber("CourseCredits", value.CourseCredits);
        writer.WriteString("CourseId", value.CourseId);

        writer.WritePropertyName("ExamTime");
        if (value.ExamTime != null)
        {
            JsonSerializer.Serialize(writer, value.ExamTime, options);
        }
        else
        {
            writer.WriteNullValue();
        }

        writer.WritePropertyName("Instructors");
        JsonSerializer.Serialize(writer, value.Instructors, options);

        writer.WriteString("Semesters", value.Semesters);

        writer.WritePropertyName("ScheduleAndLocations");
        writer.WriteStartArray();
        foreach (var (schedule, location) in value.ScheduleAndLocations)
        {
            writer.WriteStartObject();
            writer.WriteString("Schedule", schedule);
            writer.WriteString("Location", location);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteString("TeachingForm", value.TeachingForm);
        writer.WriteString("TeachingMethod", value.TeachingMethod);
        writer.WriteBoolean("IsInternational", value.IsInternational);

        writer.WritePropertyName("ExtraProperties");
        JsonSerializer.Serialize(writer, value.ExtraProperties, options);

        writer.WriteEndObject();
    }

    private static HashSet<string> ReadStringArray(ref Utf8JsonReader reader)
    {
        var result = new HashSet<string>();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            { break; }

            if (reader.TokenType == JsonTokenType.String)
            {
                result.Add(reader.GetString()!);
            }
        }

        return result;
    }

    private static HashSet<(string Schedule, string Location)> ReadScheduleAndLocations(ref Utf8JsonReader reader)
    {
        var result = new HashSet<(string Schedule, string Location)>();

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            { break; }

            if (reader.TokenType != JsonTokenType.StartObject)
            { continue; }

            var schedule = string.Empty;
            var location = string.Empty;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                { continue; }

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Schedule":
                        schedule = reader.GetString() ?? string.Empty;
                        break;
                    case "Location":
                        location = reader.GetString() ?? string.Empty;
                        break;
                }
            }

            result.Add((schedule, location));
        }

        return result;
    }
}