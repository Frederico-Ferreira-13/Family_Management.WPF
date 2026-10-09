using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public IAccountRepository Accounts { get; }
    public IUserRepository Users { get; }
    public IBudgetRepository Budgets { get; }
    public ICategoryRepository Categories { get; }
    public ICurrencyRepository Currencies { get; }
    public IFamilyRepository Families { get; }
    public IGoalRepository Goals { get; }
    public IInvestmentRepository Investments { get; }
    public IRecurringTransactionRepository RecurringTransactions { get; }
    public ITransactionRepository Transactions { get; }
    public IUserSettingRepository UserSettings { get; }
    public IUserRoleRepository UserRoles { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        ILogger<UnitOfWork> logger,
        IAccountRepository accounts,
        IUserRepository users,
        IBudgetRepository budgets,
        ICategoryRepository categories,
        ICurrencyRepository currencies,
        IFamilyRepository families,
        IGoalRepository goals,
        IInvestmentRepository investments,
        IRecurringTransactionRepository recurringTransactions,
        ITransactionRepository transactions,
        IUserSettingRepository userSettings,
        IUserRoleRepository userRoles)
    {
        _context = context;
        _logger = logger;

        Accounts = accounts;
        Users = users;
        Budgets = budgets;
        Categories = categories;
        Currencies = currencies;
        Families = families;
        Goals = goals;
        Investments = investments;
        RecurringTransactions = recurringTransactions;
        Transactions = transactions;
        UserSettings = userSettings;
        UserRoles = userRoles;
    }

    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        var transaction =
            await _context.Database.BeginTransactionAsync();

        return new DbTransaction(transaction);
    }

    public async Task<int> CompleteAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(
                ex,
                "Ocorreu um conflito de concorrência ao persistir alterações.");

            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Ocorreu um erro ao persistir alterações na base de dados.");

            throw;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}