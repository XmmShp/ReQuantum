using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 更新待办事项
/// </summary>
public class UpdateTodo(ICalendarTodoRepository repository, IUnitOfWork uow) : IRequestHandler<UpdateTodoRequest>
{
    public async Task<Result> HandleAsync(UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarTodoId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail(CalendarFailures.TodoNotFound);
        }

        existing.Update(request.Content, request.DueTime);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
