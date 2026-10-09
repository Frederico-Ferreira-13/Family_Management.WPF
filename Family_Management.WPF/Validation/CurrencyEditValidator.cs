namespace Family_Management.WPF.Validation;

public sealed class CurrencyEditValidator : ValidatorBase
{
    public void ValidateCode(string? code)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "O código da moeda é obrigatório.";
        }
        else if (code.Trim().Length != 3)
        {
            error = "O código deve ter exatamente 3 caracteres (ex.: EUR).";
        }

        SetError("Code", error);
    }

    public void ValidateName(string? name)
    {
        var error = string.IsNullOrWhiteSpace(name)
            ? "O nome da moeda é obrigatório."
            : null;

        SetError("Name", error);
    }

    public void ValidateDecimalPlaces(int decimalPlaces)
    {
        var error = decimalPlaces is < 0 or > 4
            ? "As casas decimais devem estar entre 0 e 4."
            : null;

        SetError("DecimalPlaces", error);
    }
}