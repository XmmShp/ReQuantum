using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 删除便签
/// </summary>
public record DeleteNoteRequest(long Id) : IRequest;
