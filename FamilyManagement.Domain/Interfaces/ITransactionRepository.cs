using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(
        Guid userId,
        int? limit = null);

    Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId);

    Task<IEnumerable<Transaction>> GetByDateRangeAsync(
        Guid userId,
        DateTime start,
        DateTime end);

    Task<Transaction?> GetByIdWithDetailsAsync(
        Guid id);

    Task<IEnumerable<Transaction>> GetByUserIdWithDetailsAsync(
        Guid userId,
        TransactionFilter? filter = null);

    Task<IEnumerable<Transaction>> GetByFamilyIdWithDetailsAsync(
        Guid familyId,
        DateTime start,
        DateTime end);

    Task<IEnumerable<Transaction>> SearchAsync(
        Guid userId,
        string term);

    Task<IEnumerable<Transaction>> GetUnconfirmedAsync(
        Guid userId);

    Task<decimal> GetTotalAmountByTypeAsync(
        Guid userId,
        TransactionType type,
        DateTime start,
        DateTime end);

    Task<decimal> GetAccountBalanceAsOfDateAsync(
        Guid accountId,
        DateTime asOfDate);
}