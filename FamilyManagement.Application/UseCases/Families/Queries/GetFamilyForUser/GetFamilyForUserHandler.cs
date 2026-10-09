using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Queries.GetFamilyForUser;

public sealed class GetFamilyForUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFamilyForUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FamilyDTO>> HandleAsync(
        GetFamilyForUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.FamilyId.HasValue)
        {
            return Error.NotFound(
                "Family.UserHasNoFamily",
                "O utilizador não pertence a nenhuma família.");
        }

        var family = await _unitOfWork.Families
            .GetByIdWithDetailsAsync(user.FamilyId.Value);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        return FamilyMapper.ToDTO(family);
    }
}