using FamilyManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface IAccountRepository : IRepository<Account>    
{
    
    Task<bool> AccountNameExistsAsync(string name, Guid userId, Guid accountIdToExclude);

    Task<IEnumerable<Account>> GetAccountsByUserIdAsync(Guid userId);
    Task<IEnumerable<Account>> GetUserActiveAccountsAsync(Guid userId);

    Task<IEnumerable<Account>> GetAccountsByFamilyIdAsync(Guid familyId);
    Task<IEnumerable<Account>> GetFamilyActiveAccountsAsync(Guid familyId);

    Task<Account?> GetAccountByIdWithDetailsAsync(Guid accountId);

    Task<decimal> GetTotalBalanceByUserIdAsync(Guid userId);

    Task<IEnumerable<Account>> FindAccountsWithDetailsAsync(Expression<Func<Account, bool>> predicate);
}