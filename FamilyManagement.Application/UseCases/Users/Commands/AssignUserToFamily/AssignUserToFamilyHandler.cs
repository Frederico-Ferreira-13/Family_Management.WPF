using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.AssignUserToFamily;

public sealed class AssignUserToFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignUserToFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        AssignUserToFamilyCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
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

        var family =
            await _unitOfWork.Families.GetByIdAsync(
                command.FamilyId);

        if (family is null || !family.IsActive)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        var result =
            user.AssignToFamily(
                command.FamilyId);

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}