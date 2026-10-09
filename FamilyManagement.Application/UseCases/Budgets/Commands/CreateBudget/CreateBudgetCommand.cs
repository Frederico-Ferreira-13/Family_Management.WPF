namespace FamilyManagement.Application.UseCases.Budgets.Commands.CreateBudget;

public sealed record CreateBudgetCommand(
    string Name,
    decimal Amount,
    string Currency,
    int Month,
    int Year,
    Guid CategoryId,
    Guid UserId);