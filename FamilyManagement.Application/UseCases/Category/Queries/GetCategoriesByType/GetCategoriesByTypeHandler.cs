using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetCategoriesByType;

public sealed class GetCategoriesByTypeHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesByTypeHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<CategoryDTO>>> HandleAsync(
        GetCategoriesByTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (!Enum.IsDefined(query.Type) ||
            query.Type == CategoryType.NotSpecified)
        {
            return Error.Validation(
                "O tipo de categoria é inválido.");
        }

        var categories = await _unitOfWork.Categories.FindAsync(c =>
            c.FamilyId == query.FamilyId &&
            c.Type == query.Type &&
            c.IsActive);

        var result = categories
            .Select(c => CategoryMapper.ToDTO(
                c,
                includeSubCategories: false))
            .ToList();

        return result;
    }
}