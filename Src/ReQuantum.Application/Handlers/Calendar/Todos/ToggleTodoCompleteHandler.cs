using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 切换待办完成状态
/// </summary>
public class ToggleTodoComplete : IRequestHandler<ToggleTodoCompleteRequest>
{
    private readonly ICalendarTodoRepository _repository;

    public ToggleTodoComplete(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> HandleAsync(ToggleTodoCompleteRequest request, CancellationToken cancellationToken)
    {
        var todo = _repository.FindById(CalendarTodoId.Of(request.Id));
        if (todo is null)
        {
            return Result.Fail(CalendarFailures.TodoNotFound);
        }

        todo.ToggleComplete();
        _repository.AddOrUpdate(todo);
        return Result.Success();
    }
}
