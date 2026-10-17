
using System;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.FinancialPlans;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FamilyManagement.Tests.Integration;

public sealed class FinancialPlanIntegrationTests
{
    [Fact]
    public async Task ReceitaSemConta_DeveContarParaPlanoFinanceiro()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var planResult = FinancialPlan.Create(
            name: "Família Ferreira",
            currencyCode: "EUR",
            familyId: familyId,
            createdByUserId: userId);

        Assert.True(planResult.IsSuccess);

        var plan = planResult.Value;

        var income = CreateTransaction(
            TransactionType.Income,
            1500m,
            true,
            userId,
            familyId,
            categoryId,
            plan.Id);

        var expense = CreateTransaction(
            TransactionType.Expense,
            500m,
            true,
            userId,
            familyId,
            categoryId,
            plan.Id);

        var forecast = CreateTransaction(
            TransactionType.Income,
            300m,
            false,
            userId,
            familyId,
            categoryId,
            plan.Id);

        await using (var setupContext =
            new ApplicationDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            // Isolamento do teste de agregação:
            // as entidades relacionadas não são criadas aqui.
            await setupContext.Database.ExecuteSqlRawAsync(
                "PRAGMA foreign_keys = OFF;");

            setupContext.Transactions.AddRange(
                income,
                expense,
                forecast);

            await setupContext.SaveChangesAsync();
        }

        // Novo contexto: consulta os movimentos
        // efetivamente persistidos no SQLite.
        await using var queryContext =
            new ApplicationDbContext(options);

        var transactionRepository =
            new TransactionRepository(queryContext);

        var financialPlanRepositoryMock =
            new Mock<IFinancialPlanRepository>();

        financialPlanRepositoryMock
            .Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);
        
        var familyRepositoryMock = new Mock<IFamilyRepository>();

        familyRepositoryMock
            .Setup(x => x.IsUserCreatorOfFamilyAsync(
                familyId,
                userId))
            .ReturnsAsync(true);
        

        var handler = new GetFinancialPlanSummaryHandler(
            financialPlanRepositoryMock.Object,
            transactionRepository,
            familyRepositoryMock.Object);

        var result = await handler.HandleAsync(plan.Id, userId);

        Assert.True(result.IsSuccess);

        var summary = result.Value;

        Assert.Equal(plan.Id, summary.FinancialPlanId);
        Assert.Equal("Família Ferreira", summary.Name);
        Assert.Equal("EUR", summary.CurrencyCode);

        Assert.Equal(1500m, summary.TotalIncome);
        Assert.Equal(500m, summary.TotalExpenses);
        Assert.Equal(1000m, summary.Balance);
    }

    private static Transaction CreateTransaction(
        TransactionType type,
        decimal amount,
        bool isConfirmed,
        Guid userId,
        Guid familyId,
        Guid categoryId,
        Guid financialPlanId)
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: new DateTime(2026, 10, 1),
            amount: Money.Create(amount, "EUR"),
            description: "Movimento de integração",
            type: type,
            userId: userId,
            accountId: null,
            categoryId: categoryId,
            isConfirmed: isConfirmed,
            referenceDate: new DateTime(2026, 10, 17),
            familyId: familyId,
            financialPlanId: financialPlanId);

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}
