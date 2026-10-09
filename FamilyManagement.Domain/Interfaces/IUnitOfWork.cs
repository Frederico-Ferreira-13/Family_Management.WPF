using System;
using System.Threading.Tasks;

namespace FamilyManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IAccountRepository Accounts { get; }

    IBudgetRepository Budgets { get; }

    ICategoryRepository Categories { get; }

    ICurrencyRepository Currencies { get; }

    IFamilyRepository Families { get; }

    IGoalRepository Goals { get; }

    IInvestmentRepository Investments { get; }

    IRecurringTransactionRepository RecurringTransactions { get; }

    ITransactionRepository Transactions { get; }

    IUserRepository Users { get; }

    IUserRoleRepository UserRoles { get; }

    IUserSettingRepository UserSettings { get; }

    Task<IDbTransaction> BeginTransactionAsync();

    Task<int> CompleteAsync();
}