using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 添加待办事项
/// </summary>
public class AddTodo(ICalendarTodoRepository repository, IUnitOfWork uow) : IRequestHandler<AddTodoRequest>
{
    public async Task<Result> HandleAsync(AddTodoRequest request, CancellationToken cancellationToken)
    {
        var todo = CalendarTodo.Create(request.Content, request.DueTime);
        if (request.IsCompleted)
        {
            todo.MarkCompleted();
        }

        repository.Add(todo);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
