using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.RemoveUserFromFamily;

public sealed class RemoveUserFromFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveUserFromFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        RemoveUserFromFamilyCommand command)
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

        var result =
            user.RemoveFromFamily();

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}