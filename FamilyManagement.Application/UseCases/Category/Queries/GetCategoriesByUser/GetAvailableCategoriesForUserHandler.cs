using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;

public sealed class GetAvailableCategoriesForUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAvailableCategoriesForUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<CategoryDTO>>> HandleAsync(
        GetAvailableCategoriesForUserQuery query,
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

        var familyId = user.FamilyId;

        var categories = await _unitOfWork.Categories.FindAsync(c =>
            c.IsActive &&
            (
                c.UserId == query.UserId ||
                (familyId.HasValue &&
                 c.FamilyId == familyId.Value) ||
                (!c.UserId.HasValue &&
                 !c.FamilyId.HasValue)
            ));

        var result = categories
            .Select(c => CategoryMapper.ToDTO(
                c,
                includeSubCategories: false))
            .ToList();

        return result;
    }
}