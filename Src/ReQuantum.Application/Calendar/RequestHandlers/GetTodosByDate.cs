using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 按日期获取待办事项
/// </summary>
public class GetTodosByDate(ICalendarTodoRepository repository) : IRequestHandler<GetTodosByDateRequest, GetTodosByDateResponse>
{
    public async Task<Result<GetTodosByDateResponse>> HandleAsync(GetTodosByDateRequest request, CancellationToken cancellationToken)
    {
        var todos = new List<CalendarTodoDto>();
        await foreach (var todo in repository.FindByDateAsync(request.Date, cancellationToken))
        {
            todos.Add(todo.Map.To<CalendarTodoDto>());
        }

        return new GetTodosByDateResponse(todos);
    }
}
