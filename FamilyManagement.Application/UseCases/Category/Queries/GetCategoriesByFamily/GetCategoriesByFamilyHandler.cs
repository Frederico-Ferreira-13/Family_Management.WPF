using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetCategoriesByFamily;

public sealed class GetCategoriesByFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesByFamilyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<CategoryDTO>>> HandleAsync(
        GetCategoriesByFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var categories = await _unitOfWork.Categories.FindAsync(c =>
            c.FamilyId == query.FamilyId &&
            c.IsActive);

        var result = categories
            .Select(c => CategoryMapper.ToDTO(
                c,
                includeSubCategories: false))
            .ToList();

        return result;
    }
}