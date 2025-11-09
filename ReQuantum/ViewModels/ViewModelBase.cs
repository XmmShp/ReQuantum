using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Services;
using System;
using System.ComponentModel;
using System.Globalization;

namespace ReQuantum.ViewModels;

// Only used to mark ViewModel types
public interface IViewModel;

public abstract class ViewModelBase<TView> : ObservableObject, IViewModel, IDisposable
{
    public ILocalizer Localizer { get; }
    public IWindowService WindowService { get; }

    protected ViewModelBase()
    {
        Localizer = SingletonManager.Instance.GetInstance<ILocalizer>();
        WindowService = SingletonManager.Instance.GetInstance<IWindowService>();
        Localizer.CultureChanged += OnCultureChanged;
        WindowService.PlatformModeChanged += OnPlatformModeChanged;
    }

    protected virtual void OnCultureChanged(CultureInfo cultureInfo)
    {
        Refresh();
    }

    protected virtual void OnPlatformModeChanged(bool isDesktop)
    {
        Refresh();
    }

    protected void Refresh()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(null));
    }

    public virtual void Dispose()
    {
        Localizer.CultureChanged -= OnCultureChanged;
        WindowService.PlatformModeChanged -= OnPlatformModeChanged;
        GC.SuppressFinalize(this);
    }
}