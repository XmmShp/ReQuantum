using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ReQuantum.Modules.CourseSelection.Presentations;

public static class CourseSelectionConverters
{
    /// <summary>
    /// 将集合转换为逗号分隔的字符串
    /// </summary>
    public static readonly IValueConverter CollectionToStringConverter = new FuncValueConverter<object?, string>(value =>
    {
        if (value is IEnumerable<string> stringCollection)
        {
            return string.Join(", ", stringCollection);
        }
        return string.Empty;
    });

    /// <summary>
    /// 将布尔值转换为登录状态文本
    /// </summary>
    public static readonly IValueConverter BoolToStatusConverter = new FuncValueConverter<bool, string>(isLoggedIn =>
    {
        return isLoggedIn ? "已登录" : "未登录";
    });

    /// <summary>
    /// 将布尔值转换为状态颜色
    /// </summary>
    public static readonly IValueConverter BoolToColorConverter = new FuncValueConverter<bool, IBrush>(isLoggedIn =>
    {
        return isLoggedIn ? Brushes.Green : Brushes.Red;
    });
}
