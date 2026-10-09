using System.Globalization;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class IdToBooleanConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var hasValue = value switch
        {
            Guid guid => guid != Guid.Empty,
            int number => number > 0,
            long number => number > 0,
            null => false,
            _ => true
        };

        return Invert
            ? !hasValue
            : hasValue;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}