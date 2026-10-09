using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeactivateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories
            .GetByIdWithSubcategoriesAsync(command.CategoryId);

        if (category is null)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada.");
        }

        if (!category.IsActive)
        {
            return Result.Success();
        }

        if (category.SubCategories.Any(c => c.IsActive))
        {
            return Error.BusinessRule(
                "Category.HasSubcategories",
                "Não pode desativar uma categoria que contém subcategorias ativas.");
        }

        category.Deactivate();

        _unitOfWork.Categories.Update(category);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}