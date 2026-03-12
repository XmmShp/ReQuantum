using NOF.Contract;

namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 获取指定日期的日历综合数据
/// </summary>
[PublicApi]
public record GetCalendarDayDataRequest(DateOnly Date) : IRequest<GetCalendarDayDataResponse>;

public record GetCalendarDayDataResponse(CalendarDayData Data);
