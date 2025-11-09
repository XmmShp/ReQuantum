using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ReQuantum.ViewModels;

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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
