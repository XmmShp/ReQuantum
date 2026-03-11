using ContractCalendarEvent = ReQuantum.Contract.Calendar.CalendarEvent;
using ContractCalendarTodo = ReQuantum.Contract.Calendar.CalendarTodo;
using ContractCalendarNote = ReQuantum.Contract.Calendar.CalendarNote;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar;

/// <summary>
/// 领域实体到 Contract 类型的映射工具
/// </summary>
internal static class CalendarMapper
{
    public static ContractCalendarEvent ToContract(CalendarEvent entity) =>
        new(
            (long)entity.Id,
            entity.Content,
            entity.StartTime,
            entity.EndTime,
            entity.CreatedAt,
            entity.From,
            entity.Note,
            (int)entity.Source);

    public static ContractCalendarTodo ToContract(CalendarTodo entity) =>
        new(
            (long)entity.Id,
            entity.Content,
            entity.DueTime,
            entity.IsCompleted,
            entity.CreatedAt,
            entity.Properties);

    public static ContractCalendarNote ToContract(CalendarNote entity) =>
        new(
            (long)entity.Id,
            entity.Content,
            entity.CreatedAt);
}
