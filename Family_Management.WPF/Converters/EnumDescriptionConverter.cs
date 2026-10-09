using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Family_Management.WPF.Converters;

public sealed class EnumDescriptionConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not Enum enumValue)
        {
            return value?.ToString() ?? string.Empty;
        }

        var member = enumValue
            .GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault();

        var description = member?
            .GetCustomAttribute<DescriptionAttribute>();

        return description?.Description
               ?? enumValue.ToString();
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