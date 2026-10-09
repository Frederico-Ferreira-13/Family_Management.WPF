using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IInvestmentRepository : IRepository<Investment>
{
    Task<IEnumerable<Investment>> GetInvestmentsByUserIdAsync(Guid userId);
    Task<IEnumerable<Investment>> GetActiveInvestmentsByUserIdAsync(Guid userId);

    Task<IEnumerable<Investment>> GetInvestmentsByAccountIdAsync(Guid accountId);

    Task<Investment?> GetInvestmentByNameAndUserIdAsync(string name, Guid userId);
    Task<bool> InvestmentExistsForUserByNameAsync(string name, Guid userId);
    Task<bool> InvestmentExistsForUserByNameAndIdAsync(string name, Guid userId, Guid excludeInvestmentId);

    Task<Investment?> GetInvestmentByIdWithDetailsAsync(Guid investmentId);
    Task<IEnumerable<Investment>> GetInvestmentsByUserIdWithDetailsAsync(Guid userId);
    Task<IEnumerable<Investment>> GetActiveInvestmentsByUserIdWithDetailsAsync(Guid userId);
    Task<IEnumerable<Investment>> GetInvestmentsByAccountIdWithDetailsAsync(Guid accountId);

    Task<IEnumerable<Investment>> GetProfitableInvestmentsByUserIdAsync(Guid userId);
    Task<IEnumerable<Investment>> GetLossMakingInvestmentsByUserIdAsync(Guid userId);

    Task<IEnumerable<Investment>> FindInvestmentsWithDetailsAsync(Expression<Func<Investment, bool>> predicate);
}