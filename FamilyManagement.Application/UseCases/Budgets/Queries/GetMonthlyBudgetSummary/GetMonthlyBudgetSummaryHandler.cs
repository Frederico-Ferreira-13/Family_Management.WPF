
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetMonthlyBudgetSummary;

public sealed class GetMonthlyBudgetSummaryHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMonthlyBudgetSummaryHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<MonthlyBudgetSummaryDTO>> HandleAsync(
        GetMonthlyBudgetSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (query.Month is < 1 or > 12)
        {
            return Error.Validation(
                "O mês deve estar entre 1 e 12.");
        }

        if (query.Year is < 2000 or > 9999)
        {
            return Error.Validation(
                "O ano é inválido.");
        }

        var requestedCurrency = query.Currency?.Trim().ToUpperInvariant();

        if (requestedCurrency is not null &&
            (requestedCurrency.Length != 3 ||
             !requestedCurrency.All(char.IsLetter)))
        {
            return Error.Validation(
                "A moeda deve conter três letras.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        if (query.FamilyId.HasValue)
        {
            if (user.FamilyId != query.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(query.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Budget.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }

        var budgets = query.FamilyId.HasValue
            ? await _unitOfWork.Budgets
                .GetBudgetsByFamilyAndMonthYearAsync(
                    query.FamilyId.Value,
                    query.Month,
                    query.Year)
            : await _unitOfWork.Budgets
                .GetBudgetsByUserAndMonthYearAsync(
                    query.UserId,
                    query.Month,
                    query.Year);

        var budgetList = budgets.ToList();

        var currencies = budgetList
            .Select(b => b.BudgetedAmount.Currency.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Sem moeda explícita, não é seguro agregar
        // orçamentos de moedas diferentes.
        if (requestedCurrency is null && currencies.Count > 1)
        {
            return Error.BusinessRule(
                "Budget.MixedCurrencies",
                "Existem orçamentos em moedas diferentes. " +
                "Selecione uma moeda para consultar o resumo.");
        }

        var selectedCurrency = requestedCurrency
            ?? currencies.FirstOrDefault();

        var selectedBudgets = selectedCurrency is null
            ? budgetList
            : budgetList
                .Where(b => string.Equals(
                    b.BudgetedAmount.Currency,
                    selectedCurrency,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        decimal totalBudgeted = selectedBudgets.Sum(
            b => b.BudgetedAmount.Amount);

        decimal totalSpent = selectedBudgets.Sum(
            b => b.CurrentSpent.Amount);

        decimal remainingAmount = totalBudgeted - totalSpent;

        var dto = new MonthlyBudgetSummaryDTO(
            UserId: query.UserId,
            Month: query.Month,
            Year: query.Year,
            TotalBudgeted: totalBudgeted,
            TotalSpent: totalSpent,
            RemainingAmount: remainingAmount,
            IsOverBudget: totalSpent > totalBudgeted,
            FamilyId: query.FamilyId,
            Currency: selectedCurrency);

        return Result<MonthlyBudgetSummaryDTO>.Success(dto);
    }
}
