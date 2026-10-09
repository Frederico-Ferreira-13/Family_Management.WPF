using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class UniversalVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var isVisible = HasValue(value);

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
        return Binding.DoNothing;
    }

    private static bool HasValue(object? value)
    {
        return value switch
        {
            null => false,

            bool boolean => boolean,

            string text => !string.IsNullOrWhiteSpace(text),

            Guid guid => guid != Guid.Empty,

            int number => number != 0,

            long number => number != 0,

            decimal number => number != 0,

            double number => Math.Abs(number) > double.Epsilon,

            float number => Math.Abs(number) > float.Epsilon,

            ICollection collection => collection.Count > 0,

            IEnumerable enumerable => enumerable
                .Cast<object?>()
                .Any(),

            _ => true
        };
    }
}