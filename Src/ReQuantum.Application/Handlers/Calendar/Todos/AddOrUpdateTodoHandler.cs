using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using DomainCalendarTodo = ReQuantum.Domain.Calendar.CalendarTodo;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 添加或更新待办事项
/// </summary>
public class AddOrUpdateTodo : IRequestHandler<AddOrUpdateTodoRequest>
{
    private readonly ICalendarTodoRepository _repository;

    public AddOrUpdateTodo(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(AddOrUpdateTodoRequest request, CancellationToken cancellationToken)
    {
        if (request.Id.HasValue)
        {
            var existing = _repository.FindById(CalendarTodoId.Of(request.Id.Value));
            if (existing is not null)
            {
                existing.Update(request.Content, request.DueTime);
                if (request.IsCompleted && !existing.IsCompleted)
                {
                    existing.MarkCompleted();
                }
                else if (!request.IsCompleted && existing.IsCompleted)
                {
                    existing.MarkIncomplete();
                }

                _repository.AddOrUpdate(existing);
                return Task.FromResult(Result.Success());
            }
        }

        var todo = DomainCalendarTodo.Create(request.Content, request.DueTime);
        _repository.AddOrUpdate(todo);
        return Task.FromResult(Result.Success());
    }
}
