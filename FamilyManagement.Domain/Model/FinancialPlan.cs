using System;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class FinancialPlan : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid FamilyId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public virtual Family? Family { get; private set; }

    public virtual User? CreatedByUser { get; private set; }

    private FinancialPlan()
    {
    }

    private FinancialPlan(
        string name,
        string currencyCode,
        Guid familyId,
        Guid createdByUserId,
        string? description)
    {
        Name = name;
        CurrencyCode = currencyCode;
        FamilyId = familyId;
        CreatedByUserId = createdByUserId;
        Description = description;
    }

    public static Result<FinancialPlan> Create(
        string name,
        string currencyCode,
        Guid familyId,
        Guid createdByUserId,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome do plano financeiro é obrigatório.");
        }

        if (name.Trim().Length > 100)
        {
            return Error.Validation(
                "O nome do plano não pode ultrapassar 100 caracteres.");
        }

        var normalizedCurrency = NormalizeCurrency(currencyCode);

        if (!IsValidCurrency(normalizedCurrency))
        {
            return Error.Validation(
                "A moeda deve ter um código de três letras, como EUR.");
        }

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "A família é obrigatória.");
        }

        if (createdByUserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador criador é obrigatório.");
        }

        if (!IsValidDescription(description))
        {
            return Error.Validation(
                "A descrição não pode ultrapassar 500 caracteres.");
        }

        return new FinancialPlan(
            name.Trim(),
            normalizedCurrency,
            familyId,
            createdByUserId,
            NormalizeDescription(description));
    }

    public Result Rename(string name)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "financial_plan.inactive",
                "O plano financeiro está inativo.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome do plano financeiro é obrigatório.");
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 100)
        {
            return Error.Validation(
                "O nome do plano não pode ultrapassar 100 caracteres.");
        }

        if (Name == normalizedName)
            return Result.Success();

        Name = normalizedName;
        Touch();

        return Result.Success();
    }

    public Result UpdateDescription(string? description)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "financial_plan.inactive",
                "O plano financeiro está inativo.");
        }

        if (!IsValidDescription(description))
        {
            return Error.Validation(
                "A descrição não pode ultrapassar 500 caracteres.");
        }

        var normalizedDescription =
            NormalizeDescription(description);

        if (Description == normalizedDescription)
            return Result.Success();

        Description = normalizedDescription;
        Touch();

        return Result.Success();
    }

    private static string NormalizeCurrency(string? currencyCode)
    {
        return currencyCode?
            .Trim()
            .ToUpperInvariant() ?? string.Empty;
    }

    private static bool IsValidCurrency(string currencyCode)
    {
        return currencyCode.Length == 3 &&
               currencyCode.All(
                   character => character is >= 'A' and <= 'Z');
    }

    private static bool IsValidDescription(string? description)
    {
        return description is null ||
               description.Trim().Length <= 500;
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}