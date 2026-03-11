using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 获取所有日程事件
/// </summary>
public record GetAllEventsRequest : IRequest<GetAllEventsResponse>;

public record GetAllEventsResponse(List<CalendarEvent> Events);
