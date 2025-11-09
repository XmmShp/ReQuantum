using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Services;
using System;
using System.Globalization;

namespace ReQuantum.Infrastructure;

public partial class LocalizedText : ObservableObject, IDisposable
{
    private readonly ILocalizer _localizer;

    public LocalizedText(ILocalizer localizer)
    {
        _localizer = localizer;
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