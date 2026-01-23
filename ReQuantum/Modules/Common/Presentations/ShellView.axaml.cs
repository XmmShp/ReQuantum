using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Services;

namespace ReQuantum.Infrastructure.Presentations;

public partial class ShellView : UserControl
{
    // Converters for orientation-based layout
    public static readonly IValueConverter IsLandscapeConverter =
        new FuncValueConverter<Rect, bool>(bounds => bounds.Width > bounds.Height);

    public static readonly IValueConverter IsPortraitConverter =
        new FuncValueConverter<Rect, bool>(bounds => bounds.Height >= bounds.Width);

    // Converter for menu slide animation
    public static readonly IValueConverter MenuSlideConverter =
        new FuncValueConverter<bool, double>(isOpen => isOpen ? 0 : 500);

    // Converter for menu tooltip (Desktop: expand/collapse, Mobile: not used)
    public static readonly IValueConverter MenuTooltipConverter =
        new FuncValueConverter<bool, string>(isExpanded => isExpanded ? UIText.CollapseMenu : UIText.ExpandMenu);

    // Converter to get item at specific index
    public static readonly IValueConverter GetItemAtIndexConverter = new GetItemAtIndexConverterImpl();

    public ShellView() : this(SingletonManager.Instance.GetInstance<IWindowService>())
    {
    }

    public ShellView(IWindowService windowService)
    {
        InitializeComponent();
        this.GetObservable(BoundsProperty).Subscribe(windowService.UpdateWindowBounds);
    }

    private void MenuFabButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsMenuExpanded = !viewModel.IsMenuExpanded;
        }
    }

    private void CloseMenu_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsMenuExpanded = false;
        }
    }

    private void MenuOverlay_OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsMenuExpanded = false;
        }
    }
}

/// <summary>
/// Converter for sidebar width based on expanded state
/// </summary>
public class BoolToWidthConverter : IMultiValueConverter
{
    public double CollapsedWidth { get; set; } = 60;
    public double ExpandedWidth { get; set; } = 250;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is bool isExpanded)
        {
            return isExpanded ? ExpandedWidth : CollapsedWidth;
        }
        return CollapsedWidth; // Default to collapsed
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for percentage-based height
/// </summary>
public class PercentageHeightConverter : IMultiValueConverter
{
    public double Percentage { get; set; } = 0.5;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is double height)
        {
            return height * Percentage;
        }
        return 300.0;
    }
}

/// <summary>
/// Converter to get item at specific index from a collection
/// </summary>
public class GetItemAtIndexConverterImpl : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.IEnumerable enumerable && parameter is string indexStr && int.TryParse(indexStr, out int index))
        {
            int currentIndex = 0;
            foreach (var item in enumerable)
            {
                if (currentIndex == index)
                {
                    return item;
                }
                currentIndex++;
            }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
