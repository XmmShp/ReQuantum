using ReQuantum.Attributes;
using ReQuantum.Resources.I18n;
using System;
using System.ComponentModel;
using System.Globalization;

namespace ReQuantum.Services;

public interface ILocalizer
{
    string this[string key] { get; }
    string this[string key, params object?[] args] { get; }
    void SetCulture(string cultureName);

    event Action<CultureInfo> CultureChanged;
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(ILocalizer)])]
public class Localizer : ILocalizer, INotifyPropertyChanged
{
    public void SetCulture(string cultureName)
    {
        if (cultureName == CultureInfo.CurrentUICulture.Name && cultureName == CultureInfo.CurrentCulture.Name)
        {
            return;
        }
        var newCultureInfo = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = newCultureInfo;
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
