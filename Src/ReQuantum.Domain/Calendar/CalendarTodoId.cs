using NOF.Domain;

namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 待办事项的唯一标识
/// </summary>
[NewableValueObject]
public readonly partial struct CalendarTodoId : IValueObject<long>;
