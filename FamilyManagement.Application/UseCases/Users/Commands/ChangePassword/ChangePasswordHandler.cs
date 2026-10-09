using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.ChangePassword;

public sealed class ChangePasswordHandler
{
    private const int MinimumPasswordLength = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<Result> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return Error.Validation(
                "A palavra-passe atual é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return Error.Validation(
                "A nova palavra-passe é obrigatória.");
        }

        if (command.NewPassword.Length < MinimumPasswordLength)
        {
            return Error.Validation(
                $"A nova palavra-passe deve ter pelo menos {MinimumPasswordLength} caracteres.");
        }

        if (command.CurrentPassword == command.NewPassword)
        {
            return Error.Validation(
                "A nova palavra-passe deve ser diferente da palavra-passe atual.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O utilizador não existe ou está inativo.");
        }

        var currentPasswordIsValid =
            _passwordHasher.VerifyPassword(
                command.CurrentPassword,
                user.PasswordHash);

        if (!currentPasswordIsValid)
        {
            return Error.Validation(
                "A palavra-passe atual está incorreta.");
        }

        var newPasswordHash =
            _passwordHasher.HashPassword(
                command.NewPassword);

        var updateResult =
            user.UpdatePassword(
                newPasswordHash);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}