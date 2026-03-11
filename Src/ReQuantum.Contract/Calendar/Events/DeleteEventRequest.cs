using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 删除日程事件
/// </summary>
public record DeleteEventRequest(long Id) : IRequest;
