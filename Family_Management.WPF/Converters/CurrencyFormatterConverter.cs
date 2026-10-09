using System.Globalization;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class CurrencyFormatterConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not decimal amount)
        {
            return Binding.DoNothing;
        }

        return amount.ToString("C2", culture);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string text)
        {
            return Binding.DoNothing;
        }

        return decimal.TryParse(
            text,
            NumberStyles.Currency,
            culture,
            out var amount)
                ? amount
                : Binding.DoNothing;
    }
}