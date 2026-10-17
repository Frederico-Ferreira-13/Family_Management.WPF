
using System;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;
using Moq;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.Transactions;

public sealed class TransactionImpactServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly TransactionImpactService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly Guid _financialPlanId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();

    private static readonly DateTime TestDate =
        new(2026, 10, 1);

    private static readonly DateTime ReferenceDate =
        new(2026, 10, 17);

    public TransactionImpactServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();

        _unitOfWorkMock
            .Setup(x => x.Accounts)
            .Returns(_accountRepositoryMock.Object);

        _service = new TransactionImpactService(
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ApplyIncome_SemConta_ComPlano_DeveTerSucesso()
    {
        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            accountId: null);

        var result = await _service.ApplyAsync(transaction);

        Assert.True(result.IsSuccess);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyExpense_SemConta_ComPlano_DeveTerSucesso()
    {
        var transaction = CreateTransaction(
            TransactionType.Expense,
            500m,
            accountId: null);

        var result = await _service.ApplyAsync(transaction);

        Assert.True(result.IsSuccess);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task RevertIncome_SemConta_DeveTerSucesso()
    {
        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            accountId: null);

        var result = await _service.RevertAsync(transaction);

        Assert.True(result.IsSuccess);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task RevertExpense_SemConta_DeveTerSucesso()
    {
        var transaction = CreateTransaction(
            TransactionType.Expense,
            500m,
            accountId: null);

        var result = await _service.RevertAsync(transaction);

        Assert.True(result.IsSuccess);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyIncome_ComConta_DeveAumentarSaldo()
    {
        var account = CreateAccount(1000m);

        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            account.Id);

        SetupAccount(account);

        var result = await _service.ApplyAsync(transaction);

        Assert.True(result.IsSuccess);
        Assert.Equal(2500m, account.Balance.Amount);
    }

    [Fact]
    public async Task ApplyExpense_ComConta_DeveReduzirSaldo()
    {
        var account = CreateAccount(1000m);

        var transaction = CreateTransaction(
            TransactionType.Expense,
            500m,
            account.Id);

        SetupAccount(account);

        var result = await _service.ApplyAsync(transaction);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, account.Balance.Amount);
    }

    [Fact]
    public async Task RevertIncome_ComConta_DeveReporSaldo()
    {
        var account = CreateAccount(2500m);

        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            account.Id);

        SetupAccount(account);

        var result = await _service.RevertAsync(transaction);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance.Amount);
    }

    [Fact]
    public async Task RevertExpense_ComConta_DeveReporSaldo()
    {
        var account = CreateAccount(500m);

        var transaction = CreateTransaction(
            TransactionType.Expense,
            500m,
            account.Id);

        SetupAccount(account);

        var result = await _service.RevertAsync(transaction);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, account.Balance.Amount);
    }

    [Fact]
    public async Task ApplyIncome_NaoConfirmada_DeveFalhar()
    {
        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            accountId: null,
            isConfirmed: false);

        var result = await _service.ApplyAsync(transaction);

        Assert.True(result.IsFailure);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task RevertIncome_NaoConfirmada_NaoDeveTerImpacto()
    {
        var transaction = CreateTransaction(
            TransactionType.Income,
            1500m,
            accountId: null,
            isConfirmed: false);

        var result = await _service.RevertAsync(transaction);

        Assert.True(result.IsSuccess);

        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    private Transaction CreateTransaction(
        TransactionType type,
        decimal amount,
        Guid? accountId,
        bool isConfirmed = true)
    {
        var result = Transaction.CreateIncomeOrExpense(
            date: TestDate,
            amount: Money.Create(amount, "EUR"),
            description: "Movimento de teste",
            type: type,
            userId: _userId,
            accountId: accountId,
            categoryId: _categoryId,
            familyId: _familyId,
            financialPlanId: _financialPlanId,
            isConfirmed: isConfirmed,
            referenceDate: ReferenceDate);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    private Account CreateAccount(decimal initialBalance)
    {
        var result = Account.Create(
            name: "Conta de teste",
            initialBalance: Money.Create(initialBalance, "EUR"),
            type: AccountType.Checking,
            userId: _userId,
            familyId: _familyId);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    private void SetupAccount(Account account)
    {
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id))
            .ReturnsAsync(account);
    }
}
