using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Categories.DTOs;

namespace Family_Management.WPF.Validation;

public sealed class RecurringTransactionValidator : ValidatorBase
{
    public void ValidateDescription(
        string propertyName,
        string? value)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "A descrição é obrigatória.";
        }
        else if (value.Trim().Length < 2)
        {
            error = "A descrição deve ter pelo menos 2 caracteres.";
        }

        SetError(propertyName, error);
    }

    public void ValidateAmount(
        string propertyName,
        decimal value)
    {
        SetError(
            propertyName,
            value <= 0
                ? "O valor deve ser superior a zero."
                : null);
    }

    public void ValidateEndDate(
        string propertyName,
        DateTime startDate,
        DateTime? endDate)
    {
        string? error = null;

        if (endDate.HasValue &&
            endDate.Value.Date < startDate.Date)
        {
            error =
                "A data de fim não pode ser anterior à data de início.";
        }

        SetError(propertyName, error);
    }

    public void ValidateAccount(
        string propertyName,
        AccountDTO? account)
    {
        SetError(
            propertyName,
            account is null
                ? "A conta é obrigatória."
                : null);
    }

    public void ValidateCategory(
        string propertyName,
        CategoryDTO? category)
    {
        SetError(
            propertyName,
            category is null
                ? "A categoria é obrigatória."
                : null);
    }

    public void ClearCreateErrors()
    {
        ClearErrors("NewDescription");
        ClearErrors("NewAmount");
        ClearErrors("NewEndDate");
        ClearErrors("NewSelectedAccount");
        ClearErrors("NewSelectedCategory");
    }

    public void ClearEditErrors()
    {
        ClearErrors("EditDescription");
        ClearErrors("EditAmount");
        ClearErrors("EditEndDate");
        ClearErrors("EditSelectedAccount");
        ClearErrors("EditSelectedCategory");
    }
}