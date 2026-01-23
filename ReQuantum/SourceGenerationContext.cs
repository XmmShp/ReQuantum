using System.Collections.Generic;
using System.Text.Json.Serialization;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.CoursesZju.Models;
using ReQuantum.Modules.Pta.Models;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.ZjuSso.Models;

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
// ZdbkCourseSchedule 中的新增模型
[JsonSerializable(typeof(Course))]
[JsonSerializable(typeof(StatefulCourse))]
[JsonSerializable(typeof(SelectableCourse))]
[JsonSerializable(typeof(Section))]
[JsonSerializable(typeof(SelectableSection))]
[JsonSerializable(typeof(SectionSnapshot))]
[JsonSerializable(typeof(TimeSlot))]
[JsonSerializable(typeof(HashSet<SelectableCourse>))]
[JsonSerializable(typeof(HashSet<SectionSnapshot>))]
[JsonSerializable(typeof(List<SelectableSection>))]
public partial class SourceGenerationContext : JsonSerializerContext;