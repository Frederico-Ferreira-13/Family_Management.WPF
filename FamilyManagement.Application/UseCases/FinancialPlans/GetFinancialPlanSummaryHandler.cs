using System;
using System.Linq;
using System.Threading.Tasks;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.FinancialPlans;

public sealed class GetFinancialPlanSummaryHandler
{
    private readonly IFinancialPlanRepository _financialPlans;
    private readonly ITransactionRepository _transactions;
    private readonly IFamilyRepository _families;

    public GetFinancialPlanSummaryHandler(
        IFinancialPlanRepository financialPlans,
        ITransactionRepository transactions,
        IFamilyRepository families)
    {
        _financialPlans = financialPlans
            ?? throw new ArgumentNullException(nameof(financialPlans));

        _transactions = transactions
            ?? throw new ArgumentNullException(nameof(transactions));

        _families = families
            ?? throw new ArgumentNullException(nameof(families));
    }

    public async Task<Result<FinancialPlanSummaryDTO>> HandleAsync(
        Guid financialPlanId,
        Guid userId)
    {
        if (financialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é obrigatório.");
        }

        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var plan = await _financialPlans
            .GetByIdAsync(financialPlanId);

        if (plan is null || !plan.IsActive)
        {
            return Error.NotFound(
                "FinancialPlan.NotFound",
                "Plano financeiro não encontrado ou inativo.");
        }

        var familyId = plan.FamilyId;

        var isCreator = await _families
            .IsUserCreatorOfFamilyAsync(familyId, userId);

        var isMember = await _families
            .IsUserMemberOfFamilyAsync(familyId, userId);

        if (!isCreator && !isMember)
        {
            return Error.NotFound(
                "FinancialPlan.NotFound",
                "Plano financeiro não encontrado.");
        }

        // Só consultar movimentos depois de validar o acesso.
        var transactions = (await _transactions
                .GetConfirmedByFinancialPlanIdAsync(financialPlanId))
            .ToList();

        // Não misturar moedas diferentes no mesmo total.
        if (transactions.Any(t =>
            !string.Equals(
                t.Amount.Currency,
                plan.CurrencyCode,
                StringComparison.OrdinalIgnoreCase)))
        {
            return Error.BusinessRule(
                "FinancialPlan.CurrencyMismatch",
                "Existem transações com uma moeda diferente " +
                "da moeda do plano financeiro.");
        }

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount.Amount);

        var summary = new FinancialPlanSummaryDTO(
            FinancialPlanId: plan.Id,
            Name: plan.Name,
            CurrencyCode: plan.CurrencyCode,
            TotalIncome: totalIncome,
            TotalExpenses: totalExpenses,
            Balance: totalIncome - totalExpenses);

        return summary;
    }
}
