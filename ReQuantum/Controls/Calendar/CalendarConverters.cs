using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ReQuantum.Controls;

public static class CalendarConverters
{
    public static readonly IValueConverter IsTodayToBackgroundConverter = new FuncValueConverter<bool, IBrush?>(isToday =>
        isToday ? new SolidColorBrush(Color.Parse("#2196F3")) : Brushes.Transparent);

    public static readonly IValueConverter IsTodayToForegroundConverter = new FuncValueConverter<bool, IBrush?>(isToday =>
        isToday ? Brushes.White : Brushes.Black);

    public static readonly IValueConverter IsCurrentMonthToOpacityConverter = new FuncValueConverter<bool, double>(isCurrentMonth =>
        isCurrentMonth ? 1.0 : 0.3);

    public static readonly IValueConverter IsTodayToFontWeightConverter = new FuncValueConverter<bool, FontWeight>(isToday =>
        isToday ? FontWeight.Bold : FontWeight.Normal);

    public static readonly IValueConverter IsSelectedToBackgroundConverter = new FuncValueConverter<bool, IBrush?>(isSelected =>
        isSelected ? new SolidColorBrush(Color.Parse("#E3F2FD")) : Brushes.Transparent);

    public static readonly IValueConverter IsSelectedToBorderBrushConverter = new FuncValueConverter<bool, IBrush?>(isSelected =>
        isSelected ? new SolidColorBrush(Color.Parse("#2196F3")) : Brushes.Transparent);

    public static readonly IValueConverter IsSelectedToBorderThicknessConverter = new FuncValueConverter<bool, Thickness>(isSelected =>
            isSelected ? new Thickness(2) : new Thickness(0));

    /// <summary>
    /// 将小时数转换为像素位置（每小时30像素）
    /// </summary>
    public static readonly IValueConverter HourToPixelConverter = new FuncValueConverter<double, double>(hour =>
        hour * 30);

    /// <summary>
    /// 生成24小时的集合
    /// </summary>
    public static readonly System.Collections.IEnumerable Hours24 = System.Linq.Enumerable.Range(0, 24);

    /// <summary>
    /// 将小时数转换为时间字符串（如 0 -> "00:00", 1 -> "01:00"）
    /// </summary>
    public static readonly IValueConverter HourToTimeStringConverter = new FuncValueConverter<int, string>(hour =>
        $"{hour:D2}:00");

    /// <summary>
    /// 周视图选中状态的边框厚度转换器（选中时左边框加粗）
    /// </summary>
    public static readonly IValueConverter WeekDaySelectedBorderThicknessConverter = new FuncValueConverter<bool, Thickness>(isSelected =>
        isSelected ? new Thickness(3, 0, 0, 0) : new Thickness(1, 0, 0, 0));

    /// <summary>
    /// 根据是否为桌面端返回MaxHeight值
    /// 桌面端：返回null（不限制高度）
    /// 移动端：返回"40vh"（限制为视口高度的40%）
    /// </summary>
    public static readonly IValueConverter IsDesktopModeToMaxHeightConverter = new FuncValueConverter<bool, string?>(isDesktop =>
        isDesktop ? null : "40vh");

    /// <summary>
    /// 将宽度转换为95%
    /// </summary>
    public static readonly IValueConverter WidthTo95PercentConverter = new FuncValueConverter<double, double>(width =>
        width * 0.95);
}
