using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Families.Queries.GetFamilyMembers;

public sealed class GetFamilyMembersHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFamilyMembersHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<FamilyMemberDTO>>> HandleAsync(
        GetFamilyMembersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var family = await _unitOfWork.Families
            .GetByIdAsync(query.FamilyId);

        if (family is null)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        var members = await _unitOfWork.Users
            .GetUsersByFamilyIdAsync(query.FamilyId);

        var result = members
            .OrderBy(user => user.UserName)
            .Select(FamilyMapper.ToMemberDTO)
            .ToList();

        return result;
    }
}