using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ReQuantum.Modules.Calendar.Presentations;

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
