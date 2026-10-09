using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.DeactivateFamily;

public sealed class DeactivateFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        DeactivateFamilyCommand command,
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
            return Result.Success();
        }

        var members = await _unitOfWork.Users
            .GetUsersByFamilyIdAsync(command.FamilyId);

        foreach (var member in members)
        {
            var removeResult =
                member.RemoveFromFamily();

            if (removeResult.IsFailure)
            {
                return removeResult.Error;
            }

            _unitOfWork.Users.Update(member);
        }

        family.Deactivate();

        _unitOfWork.Families.Update(family);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}