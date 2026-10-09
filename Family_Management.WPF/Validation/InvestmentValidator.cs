namespace Family_Management.WPF.Validation;

public sealed class InvestmentValidator : ValidatorBase
{
    public void ValidateName(string? name)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "O nome do investimento é obrigatório.";
        }
        else if (name.Trim().Length < 3)
        {
            error = "O nome deve ter pelo menos 3 caracteres.";
        }

        SetError("Name", error);
    }

    public void ValidateValue(decimal value)
    {
        var error = value <= 0
            ? "O valor deve ser superior a zero."
            : null;

        SetError("InitialAmount", error);
    }

    public void ValidateDate(DateTime date)
    {
        string? error = null;

        if (date > DateTime.Today)
        {
            error = "A data de compra não pode ser no futuro.";
        }
        else if (date < new DateTime(1900, 1, 1))
        {
            error = "A data de compra é inválida.";
        }

        SetError("PurchaseDate", error);
    }

    public void ValidateBroker(object? broker)
    {
        var error = broker is null
            ? "Deve selecionar uma corretora."
            : null;

        SetError("SelectedBroker", error);
    }
}