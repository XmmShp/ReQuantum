using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using DomainCalendarEvent = ReQuantum.Domain.Calendar.CalendarEvent;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Events;

/// <summary>
/// 添加或更新日程事件
/// </summary>
public class AddOrUpdateEvent : IRequestHandler<AddOrUpdateEventRequest>
{
    private readonly ICalendarEventRepository _repository;

    public AddOrUpdateEvent(ICalendarEventRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(AddOrUpdateEventRequest request, CancellationToken cancellationToken)
    {
        if (request.Id.HasValue)
        {
            var existing = _repository.FindById(CalendarEventId.Of(request.Id.Value));
            if (existing is not null)
            {
                existing.Update(request.Content, request.StartTime, request.EndTime, request.Note);
                _repository.AddOrUpdate(existing);
                return Task.FromResult(Result.Success());
            }
        }

        var source = (CalendarEventSource)request.Source;
        var calendarEvent = source == CalendarEventSource.Manual
            ? DomainCalendarEvent.Create(request.Content, request.StartTime, request.EndTime, request.Note)
            : DomainCalendarEvent.CreateFromSource(
                request.Content,
                request.StartTime,
                request.EndTime,
                source,
                request.From);

        _repository.AddOrUpdate(calendarEvent);
        return Task.FromResult(Result.Success());
    }
}
