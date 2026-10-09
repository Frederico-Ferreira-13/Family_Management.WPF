using System;
using System.Collections.Generic;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public CategoryType Type { get; private set; }

    public Guid? UserId { get; private set; }
    public Guid? FamilyId { get; private set; }
    public Guid? ParentCategoryId { get; private set; }

    public virtual User? User { get; private set; }
    public virtual Family? Family { get; private set; }
    public virtual Category? ParentCategory { get; private set; }

    private readonly List<Category> _subCategories = new();
    private readonly List<Budget> _budgets = new();
    private readonly List<Transaction> _transactions = new();

    private readonly List<RecurringTransaction> _recurringTransactions =
        new();

    public virtual IReadOnlyCollection<Category> SubCategories =>
        _subCategories.AsReadOnly();

    public virtual IReadOnlyCollection<Budget> Budgets =>
        _budgets.AsReadOnly();

    public virtual IReadOnlyCollection<Transaction> Transactions =>
        _transactions.AsReadOnly();

    public virtual IReadOnlyCollection<RecurringTransaction>
        RecurringTransactions =>
            _recurringTransactions.AsReadOnly();

    public bool IsGlobal =>
        !UserId.HasValue && !FamilyId.HasValue;

    private Category()
    {
    }

    private Category(
        string name,
        CategoryType type,
        Guid? userId,
        Guid? familyId,
        string? description,
        Guid? parentCategoryId)
    {
        Name = name;
        Type = type;

        UserId = userId;
        FamilyId = familyId;
        ParentCategoryId = parentCategoryId;

        Description = description;
    }

    public static Result<Category> Create(
        string name,
        CategoryType type,
        Guid? userId,
        Guid? familyId,
        string? description = null,
        Guid? parentCategoryId = null)
    {
        var detailsValidation = ValidateDetails(name, type);

        if (detailsValidation.IsFailure)
            return detailsValidation.Error;

        var ownershipValidation = ValidateOwnership(
            userId,
            familyId);

        if (ownershipValidation.IsFailure)
            return ownershipValidation.Error;

        if (parentCategoryId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da categoria principal é inválido.");
        }

        return new Category(
            name.Trim(),
            type,
            userId,
            familyId,
            NormalizeDescription(description),
            parentCategoryId);
    }

    public Result UpdateDetails(
        string name,
        string? description,
        CategoryType type,
        Guid? parentCategoryId)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "category.inactive",
                "A categoria está inativa.");
        }

        var validation = ValidateDetails(name, type);

        if (validation.IsFailure)
            return validation;

        if (parentCategoryId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da categoria principal é inválido.");
        }

        if (parentCategoryId == Id)
        {
            return Error.BusinessRule(
                "category.self_parent",
                "Uma categoria não pode ser a sua própria categoria principal.");
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        Type = type;
        ParentCategoryId = parentCategoryId;

        Touch();

        return Result.Success();
    }

    private static Result ValidateDetails(
        string name,
        CategoryType type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome da categoria é obrigatório.");
        }

        if (!Enum.IsDefined(type) ||
            type == CategoryType.NotSpecified)
        {
            return Error.Validation(
                "O tipo de categoria é inválido.");
        }

        return Result.Success();
    }

    private static Result ValidateOwnership(
        Guid? userId,
        Guid? familyId)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é inválido.");
        }

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (userId.HasValue && familyId.HasValue)
        {
            return Error.Validation(
                "Uma categoria deve pertencer a um utilizador ou a uma família.");
        }

        return Result.Success();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}