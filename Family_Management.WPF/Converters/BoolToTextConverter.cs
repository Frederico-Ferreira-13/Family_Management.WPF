using System.Globalization;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class BoolToTextConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not bool boolValue ||
            parameter is not string options)
        {
            return Binding.DoNothing;
        }

        var parts = options.Split(
            '|',
            2,
            StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            return Binding.DoNothing;
        }

        return boolValue
            ? parts[0]
            : parts[1];
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