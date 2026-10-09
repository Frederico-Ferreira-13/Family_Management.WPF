using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class DictionaryKeyToValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not IDictionary dictionary ||
            parameter is not string key ||
            string.IsNullOrWhiteSpace(key) ||
            !dictionary.Contains(key))
        {
            return string.Empty;
        }

        var dictionaryValue = dictionary[key];

        if (dictionaryValue is IEnumerable<string> errors)
        {
            return errors.FirstOrDefault()
                   ?? string.Empty;
        }

        return dictionaryValue?.ToString()
               ?? string.Empty;
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