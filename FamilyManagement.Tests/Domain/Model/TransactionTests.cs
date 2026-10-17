
using System;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using Xunit;

namespace FamilyManagement.Tests.Domain.Model;

public sealed class TransactionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly Guid _financialPlanId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    private static readonly DateTime TestDate =
        new(2026, 10, 1);

    private static readonly DateTime ReferenceDate =
        new(2026, 10, 17);

    [Fact]
    public void CreateIncome_SemConta_ComPlano_DeveTerSucesso()
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: TestDate,
            amount: Money.Create(1500m, "EUR"),
            description: "Salário",
            type: TransactionType.Income,
            userId: _userId,
            accountId: null,
            categoryId: _categoryId,
            familyId: _familyId,
            financialPlanId: _financialPlanId,
            isConfirmed: true,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);

        var transaction = result.Value;

        Assert.Null(transaction.AccountId);
        Assert.Equal(_financialPlanId, transaction.FinancialPlanId);
        Assert.Equal(_familyId, transaction.FamilyId);
        Assert.Equal(1500m, transaction.Amount.Amount);
        Assert.True(transaction.IsConfirmed);
    }

    [Fact]
    public void CreateExpense_SemConta_ComPlano_DeveTerSucesso()
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: TestDate,
            amount: Money.Create(500m, "EUR"),
            description: "Despesas familiares",
            type: TransactionType.Expense,
            userId: _userId,
            accountId: null,
            categoryId: _categoryId,
            familyId: _familyId,
            financialPlanId: _financialPlanId,
            isConfirmed: true,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);

        Assert.Null(result.Value.AccountId);
        Assert.Equal(_financialPlanId, result.Value.FinancialPlanId);
        Assert.Equal(TransactionType.Expense, result.Value.Type);
    }

    [Fact]
    public void CreateIncome_ComConta_SemPlano_DeveTerSucesso()
    {
        var accountId = Guid.NewGuid();

        var result = Transaction.CreateIncomeOrExpense(
            date: TestDate,
            amount: Money.Create(100m, "EUR"),
            description: "Receita pessoal",
            type: TransactionType.Income,
            userId: _userId,
            accountId: accountId,
            categoryId: _categoryId,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(accountId, result.Value.AccountId);
        Assert.Null(result.Value.FinancialPlanId);
    }

    [Fact]
    public void CreateIncome_SemContaESemPlano_DeveFalhar()
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: TestDate,
            amount: Money.Create(100m, "EUR"),
            description: "Receita inválida",
            type: TransactionType.Income,
            userId: _userId,
            accountId: null,
            categoryId: _categoryId,
            referenceDate: ReferenceDate);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CreateTransfer_DeveExigirContasDiferentes()
    {
        var accountId = Guid.NewGuid();

        var result = Transaction.CreateTransfer(
            date: TestDate,
            amount: Money.Create(100m, "EUR"),
            description: "Transferência",
            userId: _userId,
            sourceAccountId: accountId,
            targetAccountId: accountId,
            referenceDate: ReferenceDate);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CreateTransfer_ComDuasContas_DeveTerSucesso()
    {
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();

        var result = Transaction.CreateTransfer(
            date: TestDate,
            amount: Money.Create(100m, "EUR"),
            description: "Transferência",
            userId: _userId,
            sourceAccountId: sourceAccountId,
            targetAccountId: targetAccountId,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(sourceAccountId, result.Value.AccountId);
        Assert.Equal(targetAccountId, result.Value.TargetAccountId);
    }

    [Fact]
    public void CreateIncome_FuturaConfirmada_DeveFalhar()
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: new DateTime(2026, 11, 1),
            amount: Money.Create(100m, "EUR"),
            description: "Receita futura",
            type: TransactionType.Income,
            userId: _userId,
            accountId: null,
            categoryId: _categoryId,
            familyId: _familyId,
            financialPlanId: _financialPlanId,
            isConfirmed: true,
            referenceDate: ReferenceDate);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CreateIncome_FuturaNaoConfirmada_DeveTerSucesso()
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: new DateTime(2026, 11, 1),
            amount: Money.Create(100m, "EUR"),
            description: "Receita prevista",
            type: TransactionType.Income,
            userId: _userId,
            accountId: null,
            categoryId: _categoryId,
            familyId: _familyId,
            financialPlanId: _financialPlanId,
            isConfirmed: false,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsConfirmed);
    }
}
