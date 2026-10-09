using System;
using System.Linq;
using System.Reflection;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Application.UseCases.Setup.Interfaces;
using FamilyManagement.Application.UseCases.Setup.Services;

using Microsoft.Extensions.DependencyInjection;

namespace FamilyManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(DependencyInjection).Assembly;

        RegisterApplicationServices(services);

        RegisterHandlers(
            services,
            assembly);

        return services;
    }

    private static void RegisterApplicationServices(
        IServiceCollection services)
    {        

        services.AddScoped<ISetupService, SetupService>();

        services.AddScoped<
            ITransactionImpactService,
            TransactionImpactService>();
    }

    private static void RegisterHandlers(
        IServiceCollection services,
        Assembly assembly)
    {
        var handlerTypes = assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Name.EndsWith(
                    "Handler",
                    StringComparison.Ordinal))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }
    }
}