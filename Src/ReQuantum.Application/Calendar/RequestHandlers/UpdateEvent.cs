using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 更新日程事件
/// </summary>
public class UpdateEvent(ICalendarEventRepository repository, IUnitOfWork uow) : IRequestHandler<UpdateEventRequest>
{
    public async Task<Result> HandleAsync(UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarEventId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail(CalendarFailures.EventNotFound);
        }

        existing.Update(request.Content, request.StartTime, request.EndTime, request.Note);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
