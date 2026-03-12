using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 添加日程事件
/// </summary>
[PublicApi]
public record AddEventRequest(
    string Content,
    DateTime StartTime,
    DateTime EndTime,
    string Note,
    string From,
    int Source) : IRequest;
