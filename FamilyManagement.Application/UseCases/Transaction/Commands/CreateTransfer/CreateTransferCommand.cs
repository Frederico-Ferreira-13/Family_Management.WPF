namespace FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransfer;

public sealed record CreateTransferCommand(
    DateTime Date,
    decimal Amount,
    string Currency,
    string Description,
    string? Notes,
    Guid UserId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    bool IsConfirmed = true);