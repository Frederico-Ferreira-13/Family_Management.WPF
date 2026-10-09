using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.AddFamilyMember;

public sealed class AddFamilyMemberHandler
{
    private const int MaxFamilyMembers = 10;

    private readonly IUnitOfWork _unitOfWork;

    public AddFamilyMemberHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        AddFamilyMemberCommand command,
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
                "Não é possível adicionar membros a uma família inativa.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Error.BusinessRule(
                "User.Inactive",
                "Um utilizador inativo não pode ser adicionado a uma família.");
        }

        if (user.HasFamily)
        {
            return Error.BusinessRule(
                "User.HasFamily",
                "O utilizador já pertence a uma família.");
        }

        var count = await _unitOfWork.Users
            .CountMembersInFamilyAsync(command.FamilyId);

        if (count >= MaxFamilyMembers)
        {
            return Error.BusinessRule(
                "Family.Full",
                "A família atingiu o limite de membros.");
        }

        var assignResult =
            user.AssignToFamily(command.FamilyId);

        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}