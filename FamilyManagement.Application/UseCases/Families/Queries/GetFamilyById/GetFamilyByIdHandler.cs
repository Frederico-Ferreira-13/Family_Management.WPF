using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Queries.GetFamilyById;

public sealed class GetFamilyByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFamilyByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FamilyDTO>> HandleAsync(
        GetFamilyByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var family = await _unitOfWork.Families
            .GetByIdWithDetailsAsync(query.FamilyId);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        return FamilyMapper.ToDTO(family);
    }
}