using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 按日期范围获取日程事件
/// </summary>
[PublicApi]
public record GetEventsByDateRangeRequest(DateOnly StartDate, DateOnly EndDate) : IRequest<GetEventsByDateRangeResponse>;

public record GetEventsByDateRangeResponse(List<CalendarEvent> Events);
