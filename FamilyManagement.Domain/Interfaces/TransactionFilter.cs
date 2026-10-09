using FamilyManagement.Domain.Enums;

namespace FamilyManagement.Domain.Interfaces;

public sealed class TransactionFilter
{
    public Guid? UserId { get; set; }

    public Guid? AccountId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? BudgetId { get; set; }

    public Guid? GoalId { get; set; }

    public TransactionType? Type { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsConfirmed { get; set; }

    public string? SearchTerm { get; set; }

    public bool IncludeDetails { get; set; }

    public int? Limit { get; set; }
}