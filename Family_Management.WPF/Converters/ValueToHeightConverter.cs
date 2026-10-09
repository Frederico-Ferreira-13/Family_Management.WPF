using System.Globalization;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class ValueToHeightConverter : IValueConverter
{
    private const double DefaultScaleLimit = 1000.0;
    private const double MinimumVisibleHeight = 2.0;

    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (!decimal.TryParse(
                value?.ToString(),
                NumberStyles.Number,
                culture,
                out var currentValue))
        {
            return 0.0;
        }

        if (!double.TryParse(
                parameter?.ToString(),
                NumberStyles.Number,
                culture,
                out var maxHeight) ||
            maxHeight <= 0)
        {
            return 0.0;
        }

        var proportion =
            (double)Math.Abs(currentValue) /
            DefaultScaleLimit;

        return Math.Max(
            MinimumVisibleHeight,
            Math.Min(
                maxHeight,
                proportion * maxHeight));
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