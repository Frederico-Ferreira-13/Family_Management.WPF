
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

    // Repositório dos planos financeiros
    public IFinancialPlanRepository FinancialPlans { get; }

    public IGoalRepository Goals { get; }

    public IGoalContributionImpactRepository GoalContributionImpacts { get; }

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
        IFinancialPlanRepository financialPlans,
        IGoalRepository goals,
        IGoalContributionImpactRepository goalContributionImpacts,
        IInvestmentRepository investments,
        IRecurringTransactionRepository recurringTransactions,
        ITransactionRepository transactions,
        IUserSettingRepository userSettings,
        IUserRoleRepository userRoles)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));

        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        Accounts = accounts
            ?? throw new ArgumentNullException(nameof(accounts));

        Users = users
            ?? throw new ArgumentNullException(nameof(users));

        Budgets = budgets
            ?? throw new ArgumentNullException(nameof(budgets));

        Categories = categories
            ?? throw new ArgumentNullException(nameof(categories));

        Currencies = currencies
            ?? throw new ArgumentNullException(nameof(currencies));

        Families = families
            ?? throw new ArgumentNullException(nameof(families));

        FinancialPlans = financialPlans
            ?? throw new ArgumentNullException(nameof(financialPlans));

        Goals = goals
            ?? throw new ArgumentNullException(nameof(goals));

        GoalContributionImpacts = goalContributionImpacts
            ?? throw new ArgumentNullException(nameof(goalContributionImpacts));

        Investments = investments
            ?? throw new ArgumentNullException(nameof(investments));

        RecurringTransactions = recurringTransactions
            ?? throw new ArgumentNullException(nameof(recurringTransactions));

        Transactions = transactions
            ?? throw new ArgumentNullException(nameof(transactions));

        UserSettings = userSettings
            ?? throw new ArgumentNullException(nameof(userSettings));

        UserRoles = userRoles
            ?? throw new ArgumentNullException(nameof(userRoles));
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

    public void ClearTrackedChanges()
    {
        _context.ChangeTracker.Clear();
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
