using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 获取所有待办事项
/// </summary>
public class GetAllTodos : IRequestHandler<GetAllTodosRequest, GetAllTodosResponse>
{
    private readonly ICalendarTodoRepository _repository;

    public GetAllTodos(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetAllTodosResponse>> HandleAsync(GetAllTodosRequest request, CancellationToken cancellationToken)
    {
        var todos = _repository.FindAll().Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetAllTodosResponse>>(new GetAllTodosResponse(todos));
    }
}
