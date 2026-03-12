using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 删除待办事项
/// </summary>
public class RemoveTodo(ICalendarTodoRepository repository, IUnitOfWork uow) : IRequestHandler<RemoveTodoRequest>
{
    public async Task<Result> HandleAsync(RemoveTodoRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarTodoId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail(CalendarFailures.TodoNotFound);
        }

        repository.Remove(existing);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
