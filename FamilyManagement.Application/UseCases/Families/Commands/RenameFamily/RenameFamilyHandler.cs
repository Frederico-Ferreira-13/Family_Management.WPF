using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Commands.RenameFamily;

public sealed class RenameFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RenameFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<FamilyDTO>> HandleAsync(
        RenameFamilyCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.Validation(
                "O nome da família é obrigatório.");
        }

        var name = command.Name.Trim();

        if (name.Length < 3)
        {
            return Error.Validation(
                "O nome da família deve ter pelo menos 3 caracteres.");
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
                "Não é possível alterar uma família inativa.");
        }

        if (!family.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _unitOfWork.Families
                .FamilyNameExistsAsync(
                    name,
                    family.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Family.DuplicateName",
                    "Este nome de família já está em uso.");
            }
        }

        var renameResult =
            family.Rename(name);

        if (renameResult.IsFailure)
        {
            return renameResult.Error;
        }

        _unitOfWork.Families.Update(family);

        await _unitOfWork.CompleteAsync();

        return FamilyMapper.ToDTO(family);
    }
}