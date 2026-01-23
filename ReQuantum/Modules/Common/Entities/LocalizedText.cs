using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;

namespace ReQuantum.Infrastructure.Entities;

public partial class LocalizedText : ObservableObject, IDisposable
{
    private readonly ILocalizer _localizer;

    public LocalizedText(ILocalizer? localizer = null)
    {
        _localizer = localizer ?? SingletonManager.Instance.GetInstance<ILocalizer>();
        _localizer.CultureChanged += OnCultureChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    private string? _key;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    private object?[]? _arguments;

    /// <summary>
    /// 获取本地化后的最终文本（只读）
    /// </summary>
    public string Text => string.IsNullOrEmpty(Key) ? string.Empty : _localizer[Key, Arguments ?? []];

    /// <summary>
    /// 设置本地化键 + 参数（支持格式化）
    /// </summary>
    public void Set(string key, params object?[] args)
    {
        Key = key;
        Arguments = args;
    }

    public void Set(FormattableMessage message)
    {
        Key = message.TemplateKey;
        Arguments = message.Arguments;
    }

    private void OnCultureChanged(CultureInfo cultureInfo)
    {
        OnPropertyChanged(nameof(Text));
    }

    public void Dispose()
    {
        _localizer.CultureChanged -= OnCultureChanged;
        GC.SuppressFinalize(this);
    }
}
