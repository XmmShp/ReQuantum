using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Infrastructure.Services;

namespace ReQuantum.Infrastructure.Abstractions;

// Only used to mark ViewModel types
public interface IViewModel;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    public ILocalizer Localizer { get; }
    public IWindowService WindowService { get; }
    public INavigator Navigator { get; }
    public IEventPublisher Publisher { get; }

    protected ViewModelBase(ILocalizer? localizer = null, IWindowService? windowService = null, INavigator? navigator = null, IEventPublisher? publisher = null)
    {
        Localizer = localizer ?? SingletonManager.Instance.GetInstance<ILocalizer>();
        WindowService = windowService ?? SingletonManager.Instance.GetInstance<IWindowService>();
        Navigator = navigator ?? SingletonManager.Instance.GetInstance<INavigator>();
        Publisher = publisher ?? SingletonManager.Instance.GetInstance<IEventPublisher>();

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
}

public abstract class ViewModelBase<TView> : ViewModelBase;
