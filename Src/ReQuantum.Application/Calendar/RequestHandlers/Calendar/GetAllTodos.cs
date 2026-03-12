using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 获取所有待办事项
/// </summary>
public class GetAllTodos(ICalendarTodoRepository repository) : IRequestHandler<GetAllTodosRequest, GetAllTodosResponse>
{
    public async Task<Result<GetAllTodosResponse>> HandleAsync(GetAllTodosRequest request, CancellationToken cancellationToken)
    {
        var todos = new List<CalendarTodoDto>();
        await foreach (var todo in repository.FindAllAsync(cancellationToken))
        {
            todos.Add(todo.Map.To<CalendarTodoDto>());
        }

        return new GetAllTodosResponse(todos);
    }
}
