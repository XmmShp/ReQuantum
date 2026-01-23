using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ReQuantum.Infrastructure.Presentations;

public static class ColorConverters
{
    public static readonly IValueConverter BrushToColorConverter = new FuncValueConverter<ISolidColorBrush, Color>(
        brush => brush?.Color ?? Colors.Transparent
    );

}
