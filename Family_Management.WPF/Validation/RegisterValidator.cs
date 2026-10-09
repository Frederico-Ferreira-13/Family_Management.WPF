namespace Family_Management.WPF.Validation;

public sealed class RegisterValidator : ValidatorBase
{
    public void ValidateUserName(string? userName)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(userName))
        {
            error = "O nome de utilizador é obrigatório.";
        }
        else if (userName.Trim().Length < 3)
        {
            error =
                "O nome de utilizador deve ter pelo menos 3 caracteres.";
        }

        SetError(
            "UserName",
            error);
    }

    public void ValidateEmail(string? email)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "O email é obrigatório.";
        }
        else if (!ValidationHelper.IsValidEmail(email))
        {
            error = "O email introduzido não é válido.";
        }

        SetError(
            "Email",
            error);
    }

    public void ValidatePassword(string? password)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "A palavra-passe é obrigatória.";
        }
        else if (!ValidationHelper.IsValidPassword(password))
        {
            error =
                $"A palavra-passe deve ter pelo menos " +
                $"{ValidationHelper.MinimumPasswordLength} caracteres.";
        }

        SetError(
            "Password",
            error);
    }

    public void ValidateConfirmPassword(
        string? password,
        string? confirmPassword)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            error =
                "A confirmação da palavra-passe é obrigatória.";
        }
        else if (!string.Equals(
                     password,
                     confirmPassword,
                     StringComparison.Ordinal))
        {
            error = "As palavras-passe não coincidem.";
        }

        SetError(
            "ConfirmPassword",
            error);
    }

    public void SetUserNameUnavailable()
    {
        SetError(
            "UserName",
            "Este nome de utilizador já está em uso.");
    }

    public void SetEmailUnavailable()
    {
        SetError(
            "Email",
            "Este email já se encontra registado.");
    }
}