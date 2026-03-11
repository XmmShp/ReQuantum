using NOF.Domain;

namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 便签的唯一标识
/// </summary>
[NewableValueObject]
public readonly partial struct CalendarNoteId : IValueObject<long>;
