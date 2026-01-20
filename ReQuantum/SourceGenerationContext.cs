using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.CoursesZju.Models;
using ReQuantum.Modules.Pta.Models;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.ZjuSso.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ZjuSsoState))]
[JsonSerializable(typeof(PtaState))]
[JsonSerializable(typeof(PtaProblemSetsResponse))]
[JsonSerializable(typeof(PtaProblemSet))]
[JsonSerializable(typeof(List<CalendarNote>))]
[JsonSerializable(typeof(List<CalendarTodo>))]
[JsonSerializable(typeof(List<CalendarEvent>))]
[JsonSerializable(typeof(CoursesZjuTodosResponse))]
[JsonSerializable(typeof(CoursesZjuTodoDto))]
[JsonSerializable(typeof(CoursesZjuState))]
[JsonSerializable(typeof(ZdbkSectionScheduleResponse))]
[JsonSerializable(typeof(ZdbkState))]
[JsonSerializable(typeof(ZdbkExamResponse))]
[JsonSerializable(typeof(ZdbkExamDto))]
[JsonSerializable(typeof(AcademicCalendar))]
[JsonSerializable(typeof(AcademicCalendarResponse))]
[JsonSerializable(typeof(CourseAdjustment))]
public partial class SourceGenerationContext : JsonSerializerContext;