using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Events;

/// <summary>
/// 删除日程事件
/// </summary>
[PublicApi]
public record RemoveEventRequest(long Id) : IRequest;
