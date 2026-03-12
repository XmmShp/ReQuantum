using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 更新日程事件
/// </summary>
[PublicApi]
public record UpdateEventRequest(
    long Id,
    string Content,
    DateTime StartTime,
    DateTime EndTime,
    string Note) : IRequest;
