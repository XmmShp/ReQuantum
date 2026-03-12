using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 按日期范围获取待办事项
/// </summary>
public class GetTodosByDateRange(ICalendarTodoRepository repository) : IRequestHandler<GetTodosByDateRangeRequest, GetTodosByDateRangeResponse>
{
    public async Task<Result<GetTodosByDateRangeResponse>> HandleAsync(GetTodosByDateRangeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result.Fail(CalendarFailures.InvalidDateRange);
        }

        var todos = new List<CalendarTodoDto>();
        await foreach (var todo in repository.FindByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken))
        {
            todos.Add(todo.Map.To<CalendarTodoDto>());
        }

        return new GetTodosByDateRangeResponse(todos);
    }
}
