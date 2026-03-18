using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 删除日程事件
/// </summary>
public class RemoveEvent(ICalendarEventRepository repository, IUnitOfWork uow) : IRequestHandler<RemoveEventRequest>
{
    public async Task<Result> HandleAsync(RemoveEventRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarEventId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail("404", "Event Not Found");
        }

        repository.Remove(existing);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
