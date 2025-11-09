using Avalonia;
using ReQuantum.Attributes;
using System;
using System.ComponentModel;

namespace ReQuantum.Services;

public interface IWindowService
{
    /// <summary>
    /// 当前是否为桌面模式（横屏）
    /// </summary>
    bool IsDesktopMode { get; }

    /// <summary>
    /// 当前窗口宽度
    /// </summary>
    double WindowWidth { get; }

    /// <summary>
    /// 当前窗口高度
    /// </summary>
    double WindowHeight { get; }

    /// <summary>
    /// 平台模式改变事件（横屏/竖屏切换）
    /// </summary>
    event Action<bool> PlatformModeChanged;

    /// <summary>
    /// 窗口尺寸改变事件
    /// </summary>
    event Action<Rect> WindowBoundsChanged;

    /// <summary>
    /// 更新窗口边界信息
    /// </summary>
    void UpdateWindowBounds(Rect bounds);
}

[AutoInject(Lifetime.Singleton)]
public class WindowService : IWindowService, INotifyPropertyChanged
{
    public bool IsDesktopMode { get; private set; } = true;

    public double WindowWidth { get; private set; }

    public double WindowHeight { get; private set; }

    public event Action<bool>? PlatformModeChanged;
    public event Action<Rect>? WindowBoundsChanged;

    public void UpdateWindowBounds(Rect bounds)
    {
        WindowWidth = bounds.Width;
        WindowHeight = bounds.Height;

        // 判断平台模式：横屏为桌面端，竖屏为移动端
        var newIsDesktopMode = bounds.Width > bounds.Height;

        // 触发窗口尺寸改变事件
        WindowBoundsChanged?.Invoke(bounds);

        // 如果平台模式发生改变，触发平台模式改变事件
        if (IsDesktopMode == newIsDesktopMode)
        {
            return;
        }

        IsDesktopMode = newIsDesktopMode;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        PlatformModeChanged?.Invoke(IsDesktopMode);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
