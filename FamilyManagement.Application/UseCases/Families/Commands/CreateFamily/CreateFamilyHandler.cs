using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Families.Commands.CreateFamily;

public sealed class CreateFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result<FamilyDTO>> HandleAsync(
        CreateFamilyCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.CreatorUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador criador é obrigatório.");
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

        var creator =
            await _unitOfWork.Users
                .GetByIdAsync(
                    command.CreatorUserId);

        if (creator is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador criador não encontrado.");
        }

        if (!creator.IsActive)
        {
            return Error.BusinessRule(
                "User.Inactive",
                "Um utilizador inativo não pode criar uma família.");
        }

        if (creator.HasFamily)
        {
            return Error.BusinessRule(
                "User.HasFamily",
                "O utilizador já pertence a uma família.");
        }

        var nameExists =
            await _unitOfWork.Families
                .FamilyNameExistsAsync(name);

        if (nameExists)
        {
            return Error.Conflict(
                "Family.DuplicateName",
                $"Já existe uma família com o nome '{name}'.");
        }

        var adminRole =
            await _unitOfWork.UserRoles
                .GetByNameAsync(
                    UserRole.AdminName);

        if (adminRole is null ||
            !adminRole.IsActive)
        {
            return Error.NotFound(
                "UserRole.AdminNotFound",
                "O papel de administrador não está configurado ou está inativo.");
        }

        var familyResult =
            Family.Create(
                name: name,
                creatorUserId:
                    command.CreatorUserId);

        if (familyResult.IsFailure)
        {
            return familyResult.Error;
        }

        var family =
            familyResult.Value;

        var assignFamilyResult =
            creator.AssignToFamily(
                family.Id);

        if (assignFamilyResult.IsFailure)
        {
            return assignFamilyResult.Error;
        }

        var assignRoleResult =
            creator.AssignRole(
                adminRole);

        if (assignRoleResult.IsFailure)
        {
            return assignRoleResult.Error;
        }

        await _unitOfWork.Families
            .AddAsync(family);

        _unitOfWork.Users.Update(
            creator);

        await _unitOfWork.CompleteAsync();

        return FamilyMapper.ToDTO(
            family);
    }
}