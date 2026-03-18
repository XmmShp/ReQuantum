using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 添加日程事件
/// </summary>
public class AddEvent(ICalendarEventRepository repository, IUnitOfWork uow) : IRequestHandler<AddEventRequest>
{
    public async Task<Result> HandleAsync(AddEventRequest request, CancellationToken cancellationToken)
    {
        var source = (CalendarEventSource)request.Source;
        var calendarEvent = source == CalendarEventSource.Manual
            ? CalendarEvent.Create(request.Content, request.StartTime, request.EndTime, request.Note)
            : CalendarEvent.CreateFromSource(
                request.Content,
                request.StartTime,
                request.EndTime,
                source,
                request.From);

        repository.Add(calendarEvent);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
