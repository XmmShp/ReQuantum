using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 添加或更新便签
/// </summary>
public record AddOrUpdateNoteRequest(
    long? Id,
    string Content) : IRequest;
