using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 添加或更新日程事件
/// </summary>
public record AddOrUpdateEventRequest(
    long? Id,
    string Content,
    DateTime StartTime,
    DateTime EndTime,
    string Note,
    string From,
    int Source) : IRequest;
