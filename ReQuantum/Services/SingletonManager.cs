using Microsoft.Extensions.DependencyInjection;
using System;

namespace ReQuantum.Services;

public class SingletonManager
{
    public static SingletonManager Instance { get; private set; } = new();

    private SingletonManager() { }

    private IServiceProvider? _serviceProvider;
    public void Configure(IServiceProvider serviceProvider)
    {
        _serviceProvider ??= serviceProvider;
    }

    public T GetInstance<T>() where T : notnull
    {
        return _serviceProvider is null
            ? throw new InvalidOperationException()
            : _serviceProvider.GetRequiredService<T>();
    }

    public object GetInstance(Type type)
    {
        return _serviceProvider is null
            ? throw new InvalidOperationException()
            : _serviceProvider.GetRequiredService(type);
    }
}
