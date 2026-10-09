using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class HasSelectionToBooleanConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var hasSelection = value switch
        {
            null => false,

            bool boolean => boolean,

            Guid guid => guid != Guid.Empty,

            string text => !string.IsNullOrWhiteSpace(text),

            ICollection collection => collection.Count > 0,

            IEnumerable enumerable => enumerable
                .Cast<object?>()
                .Any(),

            _ when value == DependencyProperty.UnsetValue => false,

            _ => true
        };

        return Invert
            ? !hasSelection
            : hasSelection;
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