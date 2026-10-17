using System;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.FinancialPlans;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using Moq;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.FinancialPlans;

public sealed class GetFinancialPlanSummaryHandlerTests
{
    private readonly Mock<IFinancialPlanRepository> _plans = new();
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<IFamilyRepository> _families = new();

    private GetFinancialPlanSummaryHandler CreateHandler()
    {
        return new GetFinancialPlanSummaryHandler(
            _plans.Object,
            _transactions.Object,
            _families.Object);
    }

    [Fact]
    public async Task UtilizadorSemPermissao_NaoDeveConsultarTransacoes()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = CreatePlan(familyId, userId);

        _plans
            .Setup(x => x.GetByIdAsync(plan.Id))
            .ReturnsAsync(plan);

        var result = await CreateHandler()
            .HandleAsync(plan.Id, userId);

        Assert.True(result.IsFailure);

        _transactions.Verify(
            x => x.GetConfirmedByFinancialPlanIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task PlanoInexistente_NaoDeveConsultarTransacoes()
    {
        var result = await CreateHandler()
            .HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.IsFailure);

        _transactions.Verify(
            x => x.GetConfirmedByFinancialPlanIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task IdentificadoresInvalidos_DevemSerRejeitados()
    {
        var handler = CreateHandler();

        var invalidPlan = await handler.HandleAsync(
            Guid.Empty,
            Guid.NewGuid());

        var invalidUser = await handler.HandleAsync(
            Guid.NewGuid(),
            Guid.Empty);

        Assert.True(invalidPlan.IsFailure);
        Assert.True(invalidUser.IsFailure);
    }

    private static FinancialPlan CreatePlan(
        Guid familyId,
        Guid userId)
    {
        var result = FinancialPlan.Create(
            "Família Ferreira",
            "EUR",
            familyId,
            userId);

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}