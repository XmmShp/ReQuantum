using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 切换待办完成状态
/// </summary>
public class ToggleTodoComplete(ICalendarTodoRepository repository, IUnitOfWork uow) : IRequestHandler<ToggleTodoCompleteRequest>
{
    public async Task<Result> HandleAsync(ToggleTodoCompleteRequest request, CancellationToken cancellationToken)
    {
        var todo = await repository.FindAsync(CalendarTodoId.Of(request.Id), cancellationToken);
        if (todo is null)
        {
            return Result.Fail(CalendarFailures.TodoNotFound);
        }

        todo.ToggleComplete();
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
