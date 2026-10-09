using System.Windows.Markup;

namespace Family_Management.WPF.Utilities;

[MarkupExtensionReturnType(typeof(Array))]
public sealed class EnumBindingSourceExtension : MarkupExtension
{
    private Type? _enumType;

    public EnumBindingSourceExtension()
    {
    }

    public EnumBindingSourceExtension(Type enumType)
    {
        EnumType = enumType;
    }

    public Type? EnumType
    {
        get => _enumType;

        set
        {
            if (value is not null && !value.IsEnum)
            {
                throw new ArgumentException(
                    "A propriedade EnumType deve representar um tipo Enum.",
                    nameof(value));
            }

            _enumType = value;
        }
    }

    public override object ProvideValue(
        IServiceProvider serviceProvider)
    {
        if (EnumType is null)
        {
            throw new InvalidOperationException(
                "A propriedade EnumType deve ser definida.");
        }

        return Enum.GetValues(EnumType);
    }
}