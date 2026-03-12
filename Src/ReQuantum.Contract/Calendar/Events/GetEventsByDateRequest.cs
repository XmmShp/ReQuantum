using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 按日期获取日程事件
/// </summary>
[PublicApi]
public record GetEventsByDateRequest(DateOnly Date) : IRequest<GetEventsByDateResponse>;

public record GetEventsByDateResponse(List<CalendarEvent> Events);
