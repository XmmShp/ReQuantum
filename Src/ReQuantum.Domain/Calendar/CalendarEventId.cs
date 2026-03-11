using NOF.Domain;

namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 日程事件的唯一标识
/// </summary>
[NewableValueObject]
public readonly partial struct CalendarEventId : IValueObject<long>;
