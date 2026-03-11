using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Todos;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Todos;

/// <summary>
/// 按日期范围获取待办事项
/// </summary>
public class GetTodosByDateRange : IRequestHandler<GetTodosByDateRangeRequest, GetTodosByDateRangeResponse>
{
    private readonly ICalendarTodoRepository _repository;

    public GetTodosByDateRange(ICalendarTodoRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetTodosByDateRangeResponse>> HandleAsync(GetTodosByDateRangeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Task.FromResult<Result<GetTodosByDateRangeResponse>>(
                Result.Fail(CalendarFailures.InvalidDateRange));
        }

        var todos = _repository.FindByDateRange(request.StartDate, request.EndDate).Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetTodosByDateRangeResponse>>(new GetTodosByDateRangeResponse(todos));
    }
}
