using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Extensions;
using ReQuantum.Resources.I18n;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace ReQuantum.Services;

public interface ILocalizer
{
    string this[string key] { get; }
    string this[string key, params object?[] args] { get; }
    void SetCulture(string cultureName);

    event Action<CultureInfo> CultureChanged;
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(ILocalizer), typeof(IInitializable)])]
public class Localizer : ILocalizer, INotifyPropertyChanged, IInitializable
{
    private readonly IStorage _storage;
    private const string LanguageStateKey = "Localizer:Lang";
    public Localizer(IStorage storage)
    {
        _storage = storage;
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
    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        if (_storage.TryGet<string>(LanguageStateKey, out var cultureName))
        {
            SetCulture(cultureName);
        }
        return Task.CompletedTask;
    }
}
