
using System;
using FamilyManagement.Domain.Model;
using Xunit;

namespace FamilyManagement.Tests.Domain.Model;

public sealed class FinancialPlanTests
{
    private readonly Guid _familyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_DeveCriarPlanoFinanceiroValido()
    {
        var result = FinancialPlan.Create(
            name: "Família Ferreira",
            currencyCode: "EUR",
            familyId: _familyId,
            createdByUserId: _userId,
            description: "Planeamento financeiro familiar");

        Assert.True(result.IsSuccess);

        var plan = result.Value;

        Assert.Equal("Família Ferreira", plan.Name);
        Assert.Equal("EUR", plan.CurrencyCode);
        Assert.Equal(_familyId, plan.FamilyId);
        Assert.Equal(_userId, plan.CreatedByUserId);
        Assert.True(plan.IsActive);
    }

    [Fact]
    public void Create_DeveNormalizarNomeEMoeda()
    {
        var result = FinancialPlan.Create(
            name: "  Família Ferreira  ",
            currencyCode: " eur ",
            familyId: _familyId,
            createdByUserId: _userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Família Ferreira", result.Value.Name);
        Assert.Equal("EUR", result.Value.CurrencyCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_DeveRejeitarNomeVazio(string name)
    {
        var result = FinancialPlan.Create(
            name,
            "EUR",
            _familyId,
            _userId);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("12A")]
    public void Create_DeveRejeitarMoedaInvalida(string currency)
    {
        var result = FinancialPlan.Create(
            "Família Ferreira",
            currency,
            _familyId,
            _userId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_DeveRejeitarFamiliaInvalida()
    {
        var result = FinancialPlan.Create(
            "Família Ferreira",
            "EUR",
            Guid.Empty,
            _userId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Rename_DeveAtualizarNome()
    {
        var plan = CreateValidPlan();

        var result = plan.Rename("Novo plano familiar");

        Assert.True(result.IsSuccess);
        Assert.Equal("Novo plano familiar", plan.Name);
    }

    [Fact]
    public void UpdateDescription_DeveAtualizarDescricao()
    {
        var plan = CreateValidPlan();

        var result = plan.UpdateDescription(
            "  Planeamento de receitas e despesas  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Planeamento de receitas e despesas",
            plan.Description);
    }

    private FinancialPlan CreateValidPlan()
    {
        var result = FinancialPlan.Create(
            "Família Ferreira",
            "EUR",
            _familyId,
            _userId);

        Assert.True(result.IsSuccess);

        return result.Value;
    }
}
