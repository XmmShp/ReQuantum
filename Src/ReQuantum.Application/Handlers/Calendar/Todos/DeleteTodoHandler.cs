using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 删除待办事项
/// </summary>
public class DeleteTodo : IRequestHandler<DeleteTodoRequest>
{
    private readonly ICalendarTodoRepository _repository;

    public DeleteTodo(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteTodoRequest request, CancellationToken cancellationToken)
    {
        _repository.Delete(CalendarTodoId.Of(request.Id));
        return Task.FromResult(Result.Success());
    }
}
