using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 按日期获取待办事项
/// </summary>
public class GetTodosByDate : IRequestHandler<GetTodosByDateRequest, GetTodosByDateResponse>
{
    private readonly ICalendarTodoRepository _repository;

    public GetTodosByDate(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetTodosByDateResponse>> HandleAsync(GetTodosByDateRequest request, CancellationToken cancellationToken)
    {
        var todos = _repository.FindByDate(request.Date).Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetTodosByDateResponse>>(new GetTodosByDateResponse(todos));
    }
}
