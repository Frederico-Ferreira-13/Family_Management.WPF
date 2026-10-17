
using System;
using System.Threading.Tasks;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FamilyManagement.Tests.Integration;

public sealed class TransactionPersistenceTests
{
    [Fact]
    public async Task TransacaoSemConta_DeveSerPersistida()
    {
        // SQLite em memória: nunca utiliza a BD da WPF.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var financialPlanId = Guid.NewGuid();

        var result = Transaction.CreateIncomeOrExpense(
            date: new DateTime(2026, 10, 1),
            amount: Money.Create(1500m, "EUR"),
            description: "Salário familiar",
            type: TransactionType.Income,
            userId: userId,
            accountId: null,
            categoryId: categoryId,
            isConfirmed: true,
            referenceDate: new DateTime(2026, 10, 17),
            familyId: familyId,
            financialPlanId: financialPlanId);

        Assert.True(result.IsSuccess);

        var transaction = result.Value;

        // Cria a estrutura SQLite com o modelo EF real.
        await using (var context = new ApplicationDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            // Este teste verifica a persistência das colunas
            // sem exigir a criação de todo o agregado familiar.
            // As referências são identificadores válidos,
            // mas não existem entidades relacionadas neste teste.
            //
            // Desativamos temporariamente a verificação de
            // chaves estrangeiras nesta ligação de teste.
            await context.Database.ExecuteSqlRawAsync(
                "PRAGMA foreign_keys = OFF;");

            context.Transactions.Add(transaction);

            await context.SaveChangesAsync();
        }

        // Um novo DbContext garante que os dados são
        // realmente lidos do SQLite e não do ChangeTracker.
        await using (var context = new ApplicationDbContext(options))
        {
            var persisted = await context.Transactions
                .AsNoTracking()
                .SingleAsync(t => t.Id == transaction.Id);

            Assert.Null(persisted.AccountId);
            Assert.Equal(financialPlanId, persisted.FinancialPlanId);
            Assert.Equal(familyId, persisted.FamilyId);

            Assert.Equal(1500m, persisted.Amount.Amount);
            Assert.Equal("EUR", persisted.Amount.Currency);
            Assert.Equal(TransactionType.Income, persisted.Type);
            Assert.True(persisted.IsConfirmed);
            Assert.Equal("Salário familiar", persisted.Description);
        }
    }
}
