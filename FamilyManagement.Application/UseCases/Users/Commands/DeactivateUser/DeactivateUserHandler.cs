using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.DeactivateUser;

public sealed class DeactivateUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        DeactivateUserCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var user =
            await _unitOfWork.Users.GetByIdAsync(
                command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Result.Success();
        }

        user.Deactivate();

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}