using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDTO>> HandleAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada.");
        }

        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var duplicate = await _unitOfWork.Categories.AnyAsync(c =>
            c.Id != category.Id &&
            c.Name.ToLower() == normalizedName &&
            c.UserId == category.UserId &&
            c.FamilyId == category.FamilyId &&
            c.IsActive);

        if (duplicate)
        {
            return Error.Conflict(
                "Category.DuplicateName",
                "Já existe outra categoria ativa com este nome.");
        }

        if (command.ParentCategoryId.HasValue)
        {
            if (command.ParentCategoryId.Value == category.Id)
            {
                return Error.BusinessRule(
                    "Category.SelfParent",
                    "Uma categoria não pode ser a sua própria categoria principal.");
            }

            var parent = await _unitOfWork.Categories
                .GetByIdAsync(command.ParentCategoryId.Value);

            if (parent is null || !parent.IsActive)
            {
                return Error.NotFound(
                    "Category.ParentNotFound",
                    "A categoria principal não existe ou está inativa.");
            }

            if (parent.UserId != category.UserId ||
                parent.FamilyId != category.FamilyId)
            {
                return Error.BusinessRule(
                    "Category.ScopeMismatch",
                    "A categoria principal deve pertencer ao mesmo utilizador ou família.");
            }

            if (parent.Type != command.Type)
            {
                return Error.BusinessRule(
                    "Category.TypeMismatch",
                    "A categoria principal deve ter o mesmo tipo da categoria.");
            }
        }

        var updateResult = category.UpdateDetails(
            name: command.Name,
            description: command.Description,
            type: command.Type,
            parentCategoryId: command.ParentCategoryId);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _unitOfWork.Categories.Update(category);

        await _unitOfWork.CompleteAsync();

        return CategoryMapper.ToDTO(
            category,
            includeSubCategories: false);
    }
}