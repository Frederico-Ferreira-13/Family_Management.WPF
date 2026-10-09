using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Commands.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDTO>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = command.Name.Trim().ToLowerInvariant();

        var duplicate = await _unitOfWork.Categories.AnyAsync(c =>
            c.Name.ToLower() == normalizedName &&
            c.UserId == command.UserId &&
            c.FamilyId == command.FamilyId &&
            c.IsActive);

        if (duplicate)
        {
            return Error.Conflict(
                "Category.DuplicateName",
                $"Já existe uma categoria ativa com o nome '{command.Name}'.");
        }

        if (command.UserId.HasValue)
        {
            var user = await _unitOfWork.Users
                .GetByIdAsync(command.UserId.Value);

            if (user is null || !user.IsActive)
            {
                return Error.NotFound(
                    "User.NotFound",
                    "O utilizador não existe ou está inativo.");
            }
        }

        if (command.FamilyId.HasValue)
        {
            var family = await _unitOfWork.Families
                .GetByIdAsync(command.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família não existe ou está inativa.");
            }
        }

        if (command.ParentCategoryId.HasValue)
        {
            var parent = await _unitOfWork.Categories
                .GetByIdAsync(command.ParentCategoryId.Value);

            if (parent is null || !parent.IsActive)
            {
                return Error.NotFound(
                    "Category.ParentNotFound",
                    "A categoria principal não existe ou está inativa.");
            }

            if (parent.UserId != command.UserId ||
                parent.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Category.ScopeMismatch",
                    "A categoria principal deve pertencer ao mesmo utilizador ou família.");
            }

            if (parent.Type != command.Type)
            {
                return Error.BusinessRule(
                    "Category.TypeMismatch",
                    "A categoria principal deve ter o mesmo tipo da subcategoria.");
            }
        }

        var categoryResult = Category.Create(
            name: command.Name,
            type: command.Type,
            userId: command.UserId,
            familyId: command.FamilyId,
            description: command.Description,
            parentCategoryId: command.ParentCategoryId);

        if (categoryResult.IsFailure)
        {
            return categoryResult.Error;
        }

        var category = categoryResult.Value;

        await _unitOfWork.Categories.AddAsync(category);

        await _unitOfWork.CompleteAsync();

        return CategoryMapper.ToDTO(
            category,
            includeSubCategories: false);
    }
}