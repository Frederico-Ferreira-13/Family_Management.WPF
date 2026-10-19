using System;
using System.IO;
using Microsoft.Extensions.Logging;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Common.Models;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Infrastructure.Authentication;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FamilyManagement.Infrastructure.Persistence.Seeders;

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

        services.AddScoped<CategoryDataSeeder>();

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

        var diagnosticDirectory = Path.Combine(
            Path.GetTempPath(),
            "FamilyManagement");

        Directory.CreateDirectory(diagnosticDirectory);

        var diagnosticFile = Path.Combine(
            diagnosticDirectory,
            "efcore-sql.log");

        File.AppendAllText(
            diagnosticFile,
            $"{Environment.NewLine}=== Diagnóstico iniciado: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(normalizedConnectionString);

            options.LogTo(
                message =>
                {
                    try
                    {
                        File.AppendAllText(
                            diagnosticFile,
                            message + Environment.NewLine);
                    }
                    catch (IOException)
                    {
                        // O diagnóstico não deve interromper a aplicação.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // O diagnóstico não deve interromper a aplicação.
                    }
                },
                new[]
                {
                    DbLoggerCategory.Database.Command.Name
                },
                LogLevel.Information);
        });
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

        services.AddScoped<
            IGoalContributionImpactRepository,
            GoalContributionImpactRepository>();

        services.AddScoped<IInvestmentRepository, InvestmentRepository>();

        services.AddScoped<
            IRecurringTransactionRepository,
            RecurringTransactionRepository>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserSettingRepository, UserSettingRepository>();
        services.AddScoped<IFinancialPlanRepository, FinancialPlanRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void RegisterInfrastructureServices(
    IServiceCollection services,
    IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"A secção '{JwtSettings.SectionName}' não está configurada.");

        if (!jwtSettings.IsValid())
        {
            throw new InvalidOperationException(
                "Configuração JWT inválida." +
                Environment.NewLine +
                $"Ambiente: {configuration["DOTNET_ENVIRONMENT"] ?? "Não definido"}" +
                Environment.NewLine +
                $"SecurityKey presente: {!string.IsNullOrWhiteSpace(jwtSettings.SecurityKey)}" +
                Environment.NewLine +
                $"Issuer presente: {!string.IsNullOrWhiteSpace(jwtSettings.Issuer)}" +
                Environment.NewLine +
                $"Audience presente: {!string.IsNullOrWhiteSpace(jwtSettings.Audience)}" +
                Environment.NewLine +
                $"ExpirationMinutes: {jwtSettings.ExpirationMinutes}");
        }

        services.AddSingleton(jwtSettings);

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
    }
}