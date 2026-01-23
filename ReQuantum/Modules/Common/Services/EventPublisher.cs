using System;
using Microsoft.Extensions.DependencyInjection;
using ReQuantum.Modules.Common.Attributes;

namespace ReQuantum.Infrastructure.Services;

public interface IEventPublisher
{
    void Publish<TEvent>(TEvent args) where TEvent : IEvent;
}

[AutoInject(Lifetime.Singleton)]
public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public EventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Publish<TEvent>(TEvent args) where TEvent : IEvent
    {
        var services = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var service in services)
        {
            service.Handle(args);
        }
    }
}

// ReSharper disable once TypeParameterCanBeVariant
public interface IEventHandler<TEvent> where TEvent : IEvent
{
    void Handle(TEvent @event);
}

public interface IEvent;
