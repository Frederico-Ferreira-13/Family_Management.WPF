using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoryByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDTO>> HandleAsync(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CategoryId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da categoria é obrigatório.");
        }

        var category = await _unitOfWork.Categories
            .GetByIdAsync(query.CategoryId);

        if (category is null)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada.");
        }

        return CategoryMapper.ToDTO(category);
    }
}