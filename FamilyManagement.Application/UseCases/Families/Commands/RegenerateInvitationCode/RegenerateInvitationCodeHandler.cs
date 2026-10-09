using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.RegenerateInvitationCode;

public sealed class RegenerateInvitationCodeHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateInvitationCodeHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<string>> HandleAsync(
        RegenerateInvitationCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
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
                "Não é possível gerar um código de convite para uma família inativa.");
        }

        var result =
            family.RegenerateInvitationCode();

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.Families.Update(family);

        await _unitOfWork.CompleteAsync();

        return family.InvitationCode;
    }
}