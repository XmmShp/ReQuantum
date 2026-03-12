using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 删除便签
/// </summary>
[PublicApi]
public record RemoveNoteRequest(long Id) : IRequest;
