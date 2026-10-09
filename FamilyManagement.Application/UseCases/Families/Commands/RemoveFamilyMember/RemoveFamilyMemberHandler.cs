using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.RemoveFamilyMember;

public sealed class RemoveFamilyMemberHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFamilyMemberHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        RemoveFamilyMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var family = await _unitOfWork.Families
            .GetByIdAsync(command.FamilyId);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        if (!family.IsActive)
        {
            return Error.BusinessRule(
                "Family.Inactive",
                "Não é possível remover membros de uma família inativa.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null ||
            user.FamilyId != command.FamilyId)
        {
            return Error.NotFound(
                "User.NotInFamily",
                "O utilizador não pertence a esta família.");
        }

        if (family.CreatorUserId == command.UserId)
        {
            return Error.BusinessRule(
                "Family.CreatorCannotBeRemoved",
                "O criador da família não pode ser removido.");
        }

        var count = await _unitOfWork.Users
            .CountMembersInFamilyAsync(command.FamilyId);

        if (count <= 1)
        {
            return Error.BusinessRule(
                "Family.LastMember",
                "Não pode remover o último membro. Desative a família.");
        }

        var removeResult =
            user.RemoveFromFamily();

        if (removeResult.IsFailure)
        {
            return removeResult.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}