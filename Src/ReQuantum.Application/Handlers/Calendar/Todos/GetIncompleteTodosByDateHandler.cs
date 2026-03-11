using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 获取指定日期之后的未完成待办
/// </summary>
public class GetIncompleteTodosByDate : IRequestHandler<GetIncompleteTodosByDateRequest, GetIncompleteTodosByDateResponse>
{
    private readonly ICalendarTodoRepository _repository;

    public GetIncompleteTodosByDate(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetIncompleteTodosByDateResponse>> HandleAsync(GetIncompleteTodosByDateRequest request, CancellationToken cancellationToken)
    {
        var todos = _repository.FindIncompleteByDate(request.Date).Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetIncompleteTodosByDateResponse>>(new GetIncompleteTodosByDateResponse(todos));
    }
}
