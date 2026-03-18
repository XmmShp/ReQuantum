using NOF.Application;
using ReQuantum.Domain.Calendar.AggregateRoots;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;
using CalendarNoteDto = ReQuantum.Contract.Calendar.CalendarNote;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.Calendar;

/// <summary>
/// 领域实体到 Contract 类型的映射工具
/// </summary>
[Mappable<CalendarEvent, CalendarEventDto>]
[Mappable<CalendarNote, CalendarNoteDto>]
[Mappable<CalendarTodo, CalendarTodoDto>]
public static partial class CalendarMapper;
