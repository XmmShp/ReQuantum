using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ReQuantum.Modules.Calendar.Presentations;

public static class CalendarConverters
{

    public static readonly IValueConverter IsTodayToBackgroundConverter = new FuncValueConverter<bool, IBrush?>(isToday =>
        isToday ? (Application.Current?.Resources?.TryGetResource("RegionSelectedTextBackgroundBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b ? b : new SolidColorBrush(Color.Parse("#ff0000"))) : Brushes.Transparent);


    public static readonly IValueConverter IsTodayToForegroundConverter = new FuncValueConverter<bool, object>(isToday =>
        isToday ? Brushes.White : AvaloniaProperty.UnsetValue);

    public static readonly IValueConverter IsCurrentMonthToOpacityConverter = new FuncValueConverter<bool, double>(isCurrentMonth =>
        isCurrentMonth ? 1.0 : 0.3);

    public static readonly IValueConverter IsTodayToFontWeightConverter = new FuncValueConverter<bool, FontWeight>(isToday =>
        isToday ? FontWeight.Bold : FontWeight.Normal);

    public static readonly IValueConverter IsSelectedToBackgroundConverter = new FuncValueConverter<bool, IBrush?>(isSelected =>
        isSelected ? (Application.Current?.Resources?.TryGetResource("RegionSelectedBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b ? b : new SolidColorBrush(Color.Parse("#ff0000"))) : Brushes.Transparent);


    public static readonly IValueConverter IsSelectedToBorderBrushConverter = new FuncValueConverter<bool, IBrush?>(isSelected =>
        isSelected ? (Application.Current?.Resources?.TryGetResource("RegionSelectedBorderBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b ? b : new SolidColorBrush(Color.Parse("#ff0000"))) : Brushes.Transparent);

    public static readonly IValueConverter IsSelectedToBorderThicknessConverter = new FuncValueConverter<bool, Thickness>(isSelected =>
            isSelected ? new Thickness(2) : new Thickness(0));

    public static readonly IValueConverter EventIdToBackgroundConverter = new FuncValueConverter<int, IBrush>(eventId =>
    {
        if (eventId % 12 >= 0 && eventId % 12 <= 2)
            if (Application.Current?.Resources?.TryGetResource("LightEventBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 3 && eventId % 12 <= 5)
            if (Application.Current?.Resources?.TryGetResource("LightEventBrush2", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 6 && eventId % 12 <= 8)
            if (Application.Current?.Resources?.TryGetResource("LightEventBrush3", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 9 && eventId % 12 <= 11)
            if (Application.Current?.Resources?.TryGetResource("LightEventBrush4", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        return Brushes.Red;//未能正确加载资源状况
    });
    public static readonly IValueConverter TodoIdToBackgroundConverter = new FuncValueConverter<int, IBrush>(eventId =>
    {
        if (eventId % 12 >= 0 && eventId % 12 <= 2)
            if (Application.Current?.Resources?.TryGetResource("LightTodoBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 3 && eventId % 12 <= 5)
            if (Application.Current?.Resources?.TryGetResource("LightTodoBrush2", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 6 && eventId % 12 <= 8)
            if (Application.Current?.Resources?.TryGetResource("LightTodoBrush3", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 9 && eventId % 12 <= 11)
            if (Application.Current?.Resources?.TryGetResource("LightTodoBrush4", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        return Brushes.Red;
    });
    public static readonly IValueConverter NoteIdToBackgroundConverter = new FuncValueConverter<int, IBrush>(eventId =>
    {
        if (eventId % 12 >= 0 && eventId % 12 <= 2)
            if (Application.Current?.Resources?.TryGetResource("LightNoteBrush", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 3 && eventId % 12 <= 5)
            if (Application.Current?.Resources?.TryGetResource("LightNoteBrush2", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 6 && eventId % 12 <= 8)
            if (Application.Current?.Resources?.TryGetResource("LightNoteBrush3", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        if (eventId % 12 >= 9 && eventId % 12 <= 11)
            if (Application.Current?.Resources?.TryGetResource("LightNoteBrush4", Application.Current.ActualThemeVariant, out var res) == true && res is IBrush b)
                return b;
        return Brushes.Red;
    });
    /// <summary>
    /// 将小时数转换为像素位置（每小时30像素，移动端）
    /// </summary>
    public static readonly IValueConverter HourToPixelConverter = new FuncValueConverter<double, double>(hour =>
        hour * 30);

    /// <summary>
    /// 将小时数转换为像素位置（每小时40像素，桌面端）
    /// </summary>
    public static readonly IValueConverter HourToPixelDesktopConverter = new FuncValueConverter<double, double>(hour =>
        hour * 40);

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

    /// <summary>
    /// 根据是否为桌面端返回对话框最小宽度
    /// 桌面端：返回550（固定最小宽度）
    /// 移动端：返回0（不限制最小宽度）
    /// </summary>
    public static readonly IValueConverter IsDesktopModeToDialogMinWidthConverter = new FuncValueConverter<bool, double>(isDesktop =>
        isDesktop ? 550 : 0);

    /// <summary>
    /// 根据是否为桌面端返回对话框宽度
    /// 桌面端：返回NaN（自动宽度）
    /// 移动端：返回NaN（自动宽度，但会被Margin限制）
    /// </summary>
    public static readonly IValueConverter IsDesktopModeToDialogWidthConverter = new FuncValueConverter<bool, double>(isDesktop =>
        double.NaN);

    /// <summary>
    /// 根据是否为桌面端返回对话框边距
    /// 桌面端：返回0（无边距）
    /// 移动端：返回16（左右各16像素边距）
    /// </summary>
    public static readonly IValueConverter IsDesktopModeToDialogMarginConverter = new FuncValueConverter<bool, Thickness>(isDesktop =>
        isDesktop ? new Thickness(0) : new Thickness(16, 0));
}


/// <summary>
/// 枚举相等性转换器
/// </summary>
public class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var valueStr = value.ToString();
        var paramStr = parameter.ToString();

        return string.Equals(valueStr, paramStr, StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        return null;
    }
}

/// <summary>
/// 大于转换器
/// </summary>
public class GreaterThanConverter : IValueConverter
{
    public static readonly GreaterThanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr && double.TryParse(paramStr, out var threshold))
        {
            return doubleValue > threshold;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// 小于等于转换器
/// </summary>
public class LessThanOrEqualConverter : IValueConverter
{
    public static readonly LessThanOrEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr && double.TryParse(paramStr, out var threshold))
        {
            return doubleValue <= threshold;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// DateTime 转 DateTimeOffset 转换器
/// </summary>
public class DateTimeToDateTimeOffsetConverter : IValueConverter
{
    public static readonly DateTimeToDateTimeOffsetConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.DateTime;
        }
        return DateTime.Now;
    }
}

/// <summary>
/// DateTime 转 TimeSpan 转换器（提取时间部分）
/// </summary>
public class DateTimeToTimeSpanConverter : IValueConverter
{
    public static readonly DateTimeToTimeSpanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }
        return DateTime.Now;
    }
}

/// <summary>
/// DateOnly 转 DateTimeOffset 转换器
/// </summary>
public class DateOnlyToDateTimeOffsetConverter : IValueConverter
{
    public static readonly DateOnlyToDateTimeOffsetConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue));
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            return DateOnly.FromDateTime(dateTimeOffset.DateTime);
        }
        return DateOnly.FromDateTime(DateTime.Now);
    }
}

/// <summary>
/// Bool 转删除线转换器
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
    public static readonly BoolToStrikethroughConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TextDecorations.Strikethrough : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 单向转换，不需要回转
        return AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// 字符串非空检查转换器
/// </summary>
public class StringIsNotNullOrEmptyConverter : IValueConverter
{
    public static readonly StringIsNotNullOrEmptyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// 布尔值转灰色画笔：true → 灰色，false → 使用原样式（避免文字消失）
/// </summary>
public class BoolToGrayIfTrueConverter : IValueConverter
{
    public static readonly BoolToGrayIfTrueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 当值为 true 时，返回灰色
        if (value is bool boolean && boolean)
        {
            return new SolidColorBrush(0xFF888888); // #888888 灰色
        }

        // 当值为 false 时，返回 UnsetValue → 表示“我不设置这个属性”
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
