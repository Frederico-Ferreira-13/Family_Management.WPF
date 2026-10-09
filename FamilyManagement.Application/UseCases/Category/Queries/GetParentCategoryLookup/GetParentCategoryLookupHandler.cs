using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetParentCategoryLookup;

public sealed class GetParentCategoryLookupHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetParentCategoryLookupHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<CategoryLookupDTO>>> HandleAsync(
        GetParentCategoryLookupQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(query.Type) ||
            query.Type == CategoryType.NotSpecified)
        {
            return Error.Validation(
                "O tipo de categoria é inválido.");
        }

        if (query.UserId.HasValue &&
            query.FamilyId.HasValue)
        {
            return Error.Validation(
                "O âmbito da categoria é inválido.");
        }

        var categories = await _unitOfWork.Categories.FindAsync(c =>
            c.IsActive &&
            c.UserId == query.UserId &&
            c.FamilyId == query.FamilyId &&
            c.Type == query.Type &&
            (!query.ExcludeCategoryId.HasValue ||
             c.Id != query.ExcludeCategoryId.Value));

        var result = categories
            .OrderBy(c => c.Name)
            .Select(CategoryMapper.ToLookupDTO)
            .ToList();

        return result;
    }
}