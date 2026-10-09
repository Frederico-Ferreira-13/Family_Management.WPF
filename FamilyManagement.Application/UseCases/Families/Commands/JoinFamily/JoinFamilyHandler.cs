using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.JoinFamily;

public sealed class JoinFamilyHandler
{
    private const int MaxFamilyMembers = 10;

    private readonly IUnitOfWork _unitOfWork;

    public JoinFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        JoinFamilyCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(
                command.InvitationCode))
        {
            return Error.Validation(
                "O código de convite é obrigatório.");
        }

        var code =
            command.InvitationCode
                .Trim()
                .ToUpperInvariant();

        var family =
            await _unitOfWork.Families
                .GetFamilyByInvitationCodeAsync(
                    code);

        if (family is null ||
            !family.IsActive)
        {
            return Error.Validation(
                "Código de convite inválido ou família inativa.");
        }

        var user =
            await _unitOfWork.Users
                .GetByIdAsync(
                    command.UserId);

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
                "Um utilizador inativo não pode aderir a uma família.");
        }

        if (user.HasFamily)
        {
            return Error.BusinessRule(
                "User.HasFamily",
                "O utilizador já pertence a uma família.");
        }

        var memberCount =
            await _unitOfWork.Users
                .CountMembersInFamilyAsync(
                    family.Id);

        if (memberCount >= MaxFamilyMembers)
        {
            return Error.BusinessRule(
                "Family.Full",
                "A família atingiu o limite de membros.");
        }

        var assignResult =
            user.AssignToFamily(
                family.Id);

        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        _unitOfWork.Users.Update(
            user);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}