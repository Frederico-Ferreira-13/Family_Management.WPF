using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var isVisible = value is true;

        if (Invert ||
            string.Equals(
                parameter?.ToString(),
                "Invert",
                StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not Visibility visibility)
        {
            return Binding.DoNothing;
        }

        var result = visibility == Visibility.Visible;

        return Invert
            ? !result
            : result;
    }
}