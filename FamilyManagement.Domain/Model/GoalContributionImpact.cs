using System;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public sealed class GoalContributionImpact : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public Guid GoalId { get; private set; }

    public decimal AppliedAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public bool IsApplied { get; private set; }
    public DateTime? AppliedAt { get; private set; }
    public DateTime? RevertedAt { get; private set; }

    public Transaction Transaction { get; private set; } = null!;
    public Goal Goal { get; private set; } = null!;

    private GoalContributionImpact()
    {
    }

    private GoalContributionImpact(
        Guid transactionId,
        Guid goalId,
        Money amount)
    {
        TransactionId = transactionId;
        GoalId = goalId;
        AppliedAmount = amount.Amount;
        Currency = amount.Currency;
        IsApplied = true;
        AppliedAt = DateTime.UtcNow;
    }

    public static Result<GoalContributionImpact> Create(
        Guid transactionId,
        Guid goalId,
        Money amount)
    {
        if (transactionId == Guid.Empty ||
            goalId == Guid.Empty)
        {
            return Error.Validation(
                "A transação e a meta são obrigatórias.");
        }

        if (amount is null || amount.Amount <= 0)
        {
            return Error.Validation(
                "A contribuição deve ser superior a zero.");
        }

        return new GoalContributionImpact(
            transactionId,
            goalId,
            amount);
    }

    public Result MarkReverted()
    {
        if (!IsApplied)
        {
            return Error.Conflict(
                "GoalContribution.AlreadyReverted",
                "A contribuição já foi revertida.");
        }

        IsApplied = false;
        RevertedAt = DateTime.UtcNow;
        Touch();

        return Result.Success();
    }

    public Result Reapply(Guid goalId, Money amount)
    {
        if (IsApplied)
        {
            return Error.Conflict(
                "GoalContribution.AlreadyApplied",
                "A contribuição já está aplicada.");
        }

        if (goalId == Guid.Empty ||
            amount is null ||
            amount.Amount <= 0)
        {
            return Error.Validation(
                "Os dados da contribuição são inválidos.");
        }

        GoalId = goalId;
        AppliedAmount = amount.Amount;
        Currency = amount.Currency;

        IsApplied = true;
        AppliedAt = DateTime.UtcNow;
        RevertedAt = null;

        Touch();

        return Result.Success();
    }
}
