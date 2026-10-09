namespace Family_Management.WPF.Validation;

public sealed class LoginValidator : ValidatorBase
{
    public void ValidateEmailOrUsername(string? value)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "O email ou nome de utilizador é obrigatório.";
        }
        else if (value.Trim().Length < 3)
        {
            error = "O identificador deve ter pelo menos 3 caracteres.";
        }

        SetError("EmailOrUsername", error);
    }

    public void ValidatePassword(string? password)
    {
        var error = string.IsNullOrWhiteSpace(password)
            ? "A palavra-passe é obrigatória."
            : null;

        SetError("Password", error);
    }
}