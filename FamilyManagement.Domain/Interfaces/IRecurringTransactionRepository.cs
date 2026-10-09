using System.Linq.Expressions;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IRecurringTransactionRepository
    : IRepository<RecurringTransaction>
{
    Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByUserIdAsync(Guid userId);

    Task<IEnumerable<RecurringTransaction>>
        GetActiveRecurringTransactionsByUserIdAsync(Guid userId);

    Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByAccountIdAsync(Guid accountId);

    Task<IEnumerable<RecurringTransaction>>
        GetPendingGenerationAsync(DateTime asOfDate);

    Task<bool>
        RecurringTransactionExistsForUserByDescriptionAsync(
            string description,
            Guid userId);

    Task<bool>
        RecurringTransactionExistsForUserByDescriptionAndIdAsync(
            string description,
            Guid userId,
            Guid excludeId);

    Task<RecurringTransaction?>
        GetRecurringTransactionByIdWithDetailsAsync(
            Guid recurringTransactionId);

    Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByUserIdWithDetailsAsync(
            Guid userId);

    Task<IEnumerable<RecurringTransaction>>
        GetRecurringTransactionsByAccountIdWithDetailsAsync(
            Guid accountId);

    Task<RecurringTransaction?>
        GetRecurringTransactionByIdWithGeneratedTransactionsAsync(
            Guid recurringTransactionId);

    Task<IEnumerable<RecurringTransaction>>
        FindRecurringTransactionsWithDetailsAsync(
            Expression<Func<RecurringTransaction, bool>> predicate);
}