using System;
using System.ComponentModel;
using System.Globalization;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;

namespace ReQuantum.Infrastructure.Services;

public interface ILocalizer
{
    string this[string key] { get; }
    string this[string key, params object?[] args] { get; }
    void SetCulture(string cultureName);

    event Action<CultureInfo> CultureChanged;
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(ILocalizer), typeof(IDaemonService)])]
public class Localizer : ILocalizer, INotifyPropertyChanged, IDaemonService
{
    private readonly IStorage _storage;
    private const string LanguageStateKey = "Localizer:Lang";
    public Localizer(IStorage storage)
    {
        _storage = storage;
        if (_storage.TryGet<string>(LanguageStateKey, out var cultureName) && cultureName is not null)
        {
            SetCulture(cultureName);
        }
    }
    public void SetCulture(string cultureName)
    {
        if (cultureName == CultureInfo.CurrentUICulture.Name && cultureName == CultureInfo.CurrentCulture.Name)
        {
            return;
        }

        var newCultureInfo = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = newCultureInfo;
        _storage.Set(LanguageStateKey, cultureName);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        CultureChanged?.Invoke(newCultureInfo);
    }

    public event Action<CultureInfo>? CultureChanged;

    public string this[string key] => UIText.ResourceManager.GetString(key, UIText.Culture) ?? key;

    public string this[string key, params object?[] args]
    {
        get
        {
            var value = UIText.ResourceManager.GetString(key, UIText.Culture) ?? key;
            if (args is { Length: > 0 })
            {
                value = string.Format(value, args);
            }
            return value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
