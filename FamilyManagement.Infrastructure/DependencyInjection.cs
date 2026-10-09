using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Common.Models;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Authentication;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RegisterPersistence(services, configuration);
        RegisterRepositories(services);
        RegisterInfrastructureServices(services, configuration);

        return services;
    }

    private static void RegisterPersistence(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não está configurada.");
        }

        var normalizedConnectionString =
            SqliteConnectionHelper.Normalize(
                connectionString,
                AppContext.BaseDirectory);

        services.AddDbContext<ApplicationDbContext>(
            options => options.UseSqlite(
                normalizedConnectionString));
    }

    private static void RegisterRepositories(
        IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IFamilyRepository, FamilyRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IInvestmentRepository, InvestmentRepository>();

        services.AddScoped<
            IRecurringTransactionRepository,
            RecurringTransactionRepository>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserSettingRepository, UserSettingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void RegisterInfrastructureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------------------------------------------------------
        // JWT / Autenticação
        // ---------------------------------------------------------

        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                "A secção 'JwtSettings' não está configurada.");

        if (!jwtSettings.IsValid())
        {
            throw new InvalidOperationException(
                "As definições JWT são inválidas. " +
                "Verifica SecurityKey, Issuer, Audience " +
                "e ExpirationMinutes.");
        }

        services.AddSingleton(jwtSettings);

        // ---------------------------------------------------------
        // Serviços de autenticação
        // ---------------------------------------------------------

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
    }
}