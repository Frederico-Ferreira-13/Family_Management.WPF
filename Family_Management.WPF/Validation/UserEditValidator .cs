using Family_Management.WPF.ViewModel.Users;

namespace Family_Management.WPF.Validation;

public sealed class UserEditValidator : ValidatorBase
{
    public void ValidateUserName(
        string propertyName,
        string? userName)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(userName))
        {
            error =
                "O nome de utilizador é obrigatório.";
        }
        else if (userName.Trim().Length < 3)
        {
            error =
                "O nome de utilizador deve ter pelo menos 3 caracteres.";
        }
        else if (userName.Trim().Length > 50)
        {
            error =
                "O nome de utilizador não pode exceder 50 caracteres.";
        }

        SetError(
            propertyName,
            error);
    }

    public void ValidateEmail(
        string propertyName,
        string? email)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            error =
                "O email é obrigatório.";
        }
        else if (!ValidationHelper.IsValidEmail(
                     email.Trim()))
        {
            error =
                "O email não é válido.";
        }

        SetError(
            propertyName,
            error);
    }

    public void ValidateAll(
        string? userName,
        string? email)
    {
        ValidateUserName(
            nameof(UserEditViewModel.UserName),
            userName);

        ValidateEmail(
            nameof(UserEditViewModel.Email),
            email);
    }
}