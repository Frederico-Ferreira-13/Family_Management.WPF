using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.Validation;

public sealed class TransactionValidator : ValidatorBase
{
    public void ValidateAmount(
        string propertyName,
        decimal amount)
    {
        SetError(
            propertyName,
            amount <= 0
                ? "O valor deve ser superior a zero."
                : null);
    }

    public void ValidateDescription(
        string propertyName,
        string? description)
    {
        string? error = null;

        if (string.IsNullOrWhiteSpace(description))
        {
            error = "A descrição é obrigatória.";
        }
        else if (description.Trim().Length < 2)
        {
            error =
                "A descrição deve ter pelo menos 2 caracteres.";
        }

        SetError(propertyName, error);
    }

    public void ValidateSourceAccount(
        string propertyName,
        AccountDTO? account)
    {
        SetError(
            propertyName,
            account is null
                ? "Selecione uma conta."
                : null);
    }

    public void ValidateSourceAccount(
        string propertyName,
        AccountDTO? account,
        Guid? financialPlanId,
        TransactionType transactionType)
    {
        string? error = null;

        if (transactionType == TransactionType.Transfer)
        {
            if (account is null)
            {
                error =
                    "Selecione a conta de origem da transferência.";
            }
        }
        else if (account is null &&
                 !financialPlanId.HasValue)
        {
            error =
                "Selecione uma conta ou associe o movimento " +
                "a um plano financeiro.";
        }

        SetError(propertyName, error);
    }

    public void ValidateCategory(
        string propertyName,
        TransactionType type,
        CategoryDTO? category)
    {
        if (type == TransactionType.Transfer)
        {
            ClearErrors(propertyName);
            return;
        }

        string? error = null;

        if (category is null)
        {
            error = "Selecione uma categoria.";
        }
        else
        {
            var requiredType = type switch
            {
                TransactionType.Income =>
                    CategoryType.Income,

                TransactionType.Expense =>
                    CategoryType.Expense,

                _ =>
                    CategoryType.NotSpecified
            };

            if (!category.IsActive ||
                requiredType == CategoryType.NotSpecified ||
                category.Type != requiredType)
            {
                error =
                    "A categoria selecionada não é compatível " +
                    "com o tipo de movimento.";
            }
        }

        SetError(propertyName, error);
    }

    public void ValidateTargetAccount(
        string propertyName,
        TransactionType type,
        AccountDTO? sourceAccount,
        AccountDTO? targetAccount)
    {
        if (type != TransactionType.Transfer)
        {
            ClearErrors(propertyName);
            return;
        }

        string? error = null;

        if (targetAccount is null)
        {
            error = "Selecione a conta de destino.";
        }
        else if (sourceAccount is not null &&
                 sourceAccount.Id == targetAccount.Id)
        {
            error =
                "As contas de origem e destino devem ser diferentes.";
        }
        else if (sourceAccount is not null &&
                 !string.Equals(
                     sourceAccount.Currency,
                     targetAccount.Currency,
                     StringComparison.OrdinalIgnoreCase))
        {
            error =
                "As contas da transferência devem utilizar " +
                "a mesma moeda.";
        }

        SetError(propertyName, error);
    }

    public void ValidateDate(
        string propertyName,
        DateTime date,
        bool isConfirmed)
    {
        string? error = null;

        if (date == default)
        {
            error = "A data é obrigatória.";
        }
        else if (isConfirmed &&
                 date.Date > DateTime.Today)
        {
            error =
                "Um movimento futuro deve permanecer por confirmar.";
        }

        SetError(propertyName, error);
    }

    public void ValidateRecurrenceDates(
        string propertyName,
        DateTime startDate,
        DateTime? endDate)
    {
        string? error = null;

        if (startDate == default)
        {
            error =
                "A data de início da recorrência é obrigatória.";
        }
        else if (endDate.HasValue &&
                 endDate.Value.Date < startDate.Date)
        {
            error =
                "A data de fim não pode ser anterior " +
                "à data de início.";
        }

        SetError(propertyName, error);
    }

    public void ValidateFrequency(
        string propertyName,
        Frequency frequency)
    {
        SetError(
            propertyName,
            !Enum.IsDefined(frequency)
                ? "Selecione uma periodicidade válida."
                : null);
    }

    public void ClearRecurrenceErrors()
    {
        ClearErrors("StartDate");
        ClearErrors("EndDate");
        ClearErrors("Frequency");
    }

    public void ClearTransactionErrors()
    {
        ClearErrors("Amount");
        ClearErrors("Description");
        ClearErrors("SelectedAccount");
        ClearErrors("SelectedCategory");
        ClearErrors("SelectedTargetAccount");
        ClearErrors("Date");

        ClearRecurrenceErrors();
    }
}