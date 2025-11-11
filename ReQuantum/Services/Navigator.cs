using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ReQuantum.Attributes;
using ReQuantum.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Services;

public interface INavigator
{
    IViewModel CurrentViewModel { get; }
    event Action CurrentViewModelChanged;
    event Action CurrentViewModelChanging;

    void NavigateTo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewModelType);
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(INavigator)])]
public partial class Navigator : ObservableObject, INavigator, IDisposable
{
    [ObservableProperty]
    private IViewModel _currentViewModel = null!;

    private readonly IServiceProvider _serviceProvider;

    public Navigator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        PropertyChanging += OnNavigating;
        PropertyChanged += OnNavigated;
    }

    public event Action? CurrentViewModelChanged;
    public event Action? CurrentViewModelChanging;

    public void NavigateTo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewModelType)
    {
        if (!viewModelType.IsAssignableTo(typeof(IViewModel)))
        {
            return;
        }
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (CurrentViewModel?.GetType() == viewModelType)
        {
            return;
        }

        var viewModel = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, viewModelType);
        var prevViewModel = CurrentViewModel;
        CurrentViewModel = (IViewModel)viewModel;
        (prevViewModel as IDisposable)?.Dispose();
    }

    private void OnNavigating(object? _, PropertyChangingEventArgs args)
    {
        if (args.PropertyName == nameof(CurrentViewModel))
        {
            CurrentViewModelChanging?.Invoke();
        }
    }

    private void OnNavigated(object? _, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CurrentViewModel))
        {
            CurrentViewModelChanged?.Invoke();
        }
    }

    public void Dispose()
    {
        PropertyChanging -= OnNavigating;
        PropertyChanged -= OnNavigated;
        GC.SuppressFinalize(this);
    }
}

