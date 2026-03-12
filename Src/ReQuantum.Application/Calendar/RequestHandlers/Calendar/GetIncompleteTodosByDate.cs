using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 获取指定日期之后的未完成待办
/// </summary>
public class GetIncompleteTodosByDate(ICalendarTodoRepository repository) : IRequestHandler<GetIncompleteTodosByDateRequest, GetIncompleteTodosByDateResponse>
{
    public async Task<Result<GetIncompleteTodosByDateResponse>> HandleAsync(GetIncompleteTodosByDateRequest request, CancellationToken cancellationToken)
    {
        var todos = new List<CalendarTodoDto>();
        await foreach (var todo in repository.FindIncompleteByDateAsync(request.Date, cancellationToken))
        {
            todos.Add(todo.Map.To<CalendarTodoDto>());
        }

        return new GetIncompleteTodosByDateResponse(todos);
    }
}
