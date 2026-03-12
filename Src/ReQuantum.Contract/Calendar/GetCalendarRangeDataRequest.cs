using NOF.Contract;

namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 按日期范围获取日历综合数据。
/// </summary>
[PublicApi]
public record GetCalendarRangeDataRequest(DateOnly StartDate, DateOnly EndDate) : IRequest<GetCalendarRangeDataResponse>;

/// <summary>
/// 按日期范围返回每天的日历数据字典。
/// </summary>
public record GetCalendarRangeDataResponse(Dictionary<DateOnly, CalendarDayData> DataByDate);
