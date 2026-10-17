
using System;
using System.Threading.Tasks;

using FamilyManagement.Application.UseCases.FinancialPlans;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

using Moq;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.FinancialPlans;

public sealed class GetActiveFamilyFinancialPlanHandlerTests
{
    private readonly Mock<IFamilyRepository> _families = new();

    private readonly Mock<IFinancialPlanRepository> _financialPlans = new();

    private GetActiveFamilyFinancialPlanHandler CreateHandler()
    {
        return new GetActiveFamilyFinancialPlanHandler(
            _families.Object,
            _financialPlans.Object);
    }

    [Fact]
    public async Task Creator_DeveConseguirConsultarPlano()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = CreatePlan(familyId, userId);

        _families
            .Setup(x => x.IsUserCreatorOfFamilyAsync(
                familyId, userId))
            .ReturnsAsync(true);

        _financialPlans
            .Setup(x => x.GetActiveByFamilyIdAsync(familyId))
            .ReturnsAsync(plan);

        var result = await CreateHandler()
            .HandleAsync(familyId, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(plan.Id, result.Value.Id);
    }

    [Fact]
    public async Task Membro_DeveConseguirConsultarPlano()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = CreatePlan(familyId, userId);

        _families
            .Setup(x => x.IsUserMemberOfFamilyAsync(
                familyId, userId))
            .ReturnsAsync(true);

        _financialPlans
            .Setup(x => x.GetActiveByFamilyIdAsync(familyId))
            .ReturnsAsync(plan);

        var result = await CreateHandler()
            .HandleAsync(familyId, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(plan.Id, result.Value.Id);
    }

    [Fact]
    public async Task UtilizadorSemPermissao_DeveSerRejeitado()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = await CreateHandler()
            .HandleAsync(familyId, userId);

        Assert.True(result.IsFailure);

        _financialPlans.Verify(
            x => x.GetActiveByFamilyIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task FamiliaSemPlano_DeveDevolverFalha()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _families
            .Setup(x => x.IsUserCreatorOfFamilyAsync(
                familyId, userId))
            .ReturnsAsync(true);

        var result = await CreateHandler()
            .HandleAsync(familyId, userId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task IdentificadoresInvalidos_DevemSerRejeitados()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var handler = CreateHandler();

        var invalidFamily = await handler.HandleAsync(
            Guid.Empty, userId);

        var invalidUser = await handler.HandleAsync(
            familyId, Guid.Empty);

        Assert.True(invalidFamily.IsFailure);
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
