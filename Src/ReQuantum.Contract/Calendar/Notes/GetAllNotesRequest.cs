using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 获取所有便签
/// </summary>
public record GetAllNotesRequest : IRequest<GetAllNotesResponse>;

public record GetAllNotesResponse(List<CalendarNote> Notes);
