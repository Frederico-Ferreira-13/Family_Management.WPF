namespace FamilyManagement.Application.UseCases.Budgets.Commands.UpdateBudget;

public sealed record UpdateBudgetCommand(
    Guid BudgetId,
    string Name,
    decimal Amount);