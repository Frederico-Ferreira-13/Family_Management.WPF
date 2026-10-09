using Family_Management.WPF.Services.Export;

using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Services.Threading;

using Family_Management.WPF.ViewModel.Accounts;
using Family_Management.WPF.ViewModel.Authentication;
using Family_Management.WPF.ViewModel.Budgets;
using Family_Management.WPF.ViewModel.Categories;
using Family_Management.WPF.ViewModel.Currencies;
using Family_Management.WPF.ViewModel.Dashboard;
using Family_Management.WPF.ViewModel.Families;
using Family_Management.WPF.ViewModel.Goals;
using Family_Management.WPF.ViewModel.Investments;
using Family_Management.WPF.ViewModel.RecurringTransactions;
using Family_Management.WPF.ViewModel.Shell;
using Family_Management.WPF.ViewModel.Transactions;
using Family_Management.WPF.ViewModel.Users;

using Family_Management.WPF.Views;

using FamilyManagement.Application.Common.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Family_Management.WPF.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RegisterPresentationServices(services);
        RegisterViewModels(services);
        RegisterViews(services);

        return services;
    }

    private static void RegisterPresentationServices(
        IServiceCollection services)
    {
        // ---------------------------------------------------------
        // Authentication / sessão atual
        // ---------------------------------------------------------

        services.AddSingleton<CurrentUserService>();

        services.AddSingleton<ICurrentUserService>(
            serviceProvider =>
                serviceProvider.GetRequiredService<CurrentUserService>());

        // ---------------------------------------------------------
        // Threading / Dispatcher
        // ---------------------------------------------------------

        services.AddSingleton<
            IDispatcherService,
            WpfDispatcherService>();

        // ---------------------------------------------------------
        // Navigation
        // ---------------------------------------------------------

        services.AddSingleton<
            INavigationService,
            NavigationService>();

        // ---------------------------------------------------------
        // Dialogs / Notifications
        // ---------------------------------------------------------

        services.AddSingleton<
            INotificationService,
            NotificationService>();

        // ---------------------------------------------------------
        // Exportação de transações
        // ---------------------------------------------------------

        services.AddTransient<
            ITransactionExportService,
            TransactionCsvExportService>();
    }

    private static void RegisterViewModels(
        IServiceCollection services)
    {
        // ---------------------------------------------------------
        // Shell
        // ---------------------------------------------------------

        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<AddNewSelectionViewModel>();

        // ---------------------------------------------------------
        // Authentication
        // ---------------------------------------------------------

        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ChangePasswordViewModel>();

        // ---------------------------------------------------------
        // Dashboard
        // ---------------------------------------------------------

        services.AddTransient<DashboardViewModel>();

        // ---------------------------------------------------------
        // Accounts
        // ---------------------------------------------------------

        services.AddTransient<AccountViewModel>();

        // ---------------------------------------------------------
        // Budgets
        // ---------------------------------------------------------

        services.AddTransient<BudgetViewModel>();
        services.AddTransient<BudgetFormViewModel>();

        // ---------------------------------------------------------
        // Categories
        // ---------------------------------------------------------

        services.AddTransient<CategoryViewModel>();

        // ---------------------------------------------------------
        // Currencies
        // ---------------------------------------------------------

        services.AddTransient<CurrencyViewModel>();

        // ---------------------------------------------------------
        // Families
        // ---------------------------------------------------------

        services.AddTransient<FamilyViewModel>();
        services.AddTransient<FamilySetupViewModel>();

        // ---------------------------------------------------------
        // Goals
        // ---------------------------------------------------------

        services.AddTransient<GoalViewModel>();
        services.AddTransient<GoalFormViewModel>();

        // ---------------------------------------------------------
        // Investments
        // ---------------------------------------------------------

        services.AddTransient<InvestmentViewModel>();
        services.AddTransient<InvestmentFormViewModel>();        

        // ---------------------------------------------------------
        // Recurring transactions
        // ---------------------------------------------------------

        services.AddTransient<RecurringTransactionViewModel>();

        // ---------------------------------------------------------
        // Transactions
        // ---------------------------------------------------------

        services.AddTransient<TransactionViewModel>();
        services.AddTransient<TransactionFormViewModel>();        

        // ---------------------------------------------------------
        // Users
        // ---------------------------------------------------------

        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserEditViewModel>();
    }

    private static void RegisterViews(
        IServiceCollection services)
    {
        // ---------------------------------------------------------
        // Shell
        // ---------------------------------------------------------

        services.AddTransient<MainWindow>();

        // ---------------------------------------------------------
        // Authentication
        // ---------------------------------------------------------

        services.AddTransient<LoginView>();
        services.AddTransient<RegisterView>();
        services.AddTransient<ChangePasswordWindow>();

        // ---------------------------------------------------------
        // Dashboard
        // ---------------------------------------------------------

        services.AddTransient<DashboardView>();

        // ---------------------------------------------------------
        // Accounts
        // ---------------------------------------------------------

        services.AddTransient<AccountView>();

        // ---------------------------------------------------------
        // Budgets
        // ---------------------------------------------------------

        services.AddTransient<BudgetView>();

        // ---------------------------------------------------------
        // Categories
        // ---------------------------------------------------------

        services.AddTransient<CategoryView>();

        // ---------------------------------------------------------
        // Currencies
        // ---------------------------------------------------------

        services.AddTransient<CurrencyView>();

        // ---------------------------------------------------------
        // Families
        // ---------------------------------------------------------

        services.AddTransient<FamilyView>();
        services.AddTransient<FamilySetupView>();

        // ---------------------------------------------------------
        // Goals
        // ---------------------------------------------------------

        services.AddTransient<GoalView>();

        // ---------------------------------------------------------
        // Investments
        // ---------------------------------------------------------

        services.AddTransient<InvestmentView>();
        services.AddTransient<InvestmentFormView>();

        // ---------------------------------------------------------
        // Recurring transactions
        // ---------------------------------------------------------

        services.AddTransient<RecurringTransactionView>();

        // ---------------------------------------------------------
        // Transactions
        // ---------------------------------------------------------

        services.AddTransient<TransactionView>();
        services.AddTransient<TransactionFormView>();

        // ---------------------------------------------------------
        // Users
        // ---------------------------------------------------------

        services.AddTransient<UserManagementView>();
        services.AddTransient<UserEditView>();
    }
}