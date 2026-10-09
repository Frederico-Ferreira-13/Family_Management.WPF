using FamilyManagement.Application.Common.Extensions;
using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Investments.Mapping;

public static class InvestmentMapper
{
    public static InvestmentDTO ToDTO(Investment investment)
    {
        ArgumentNullException.ThrowIfNull(investment);

        return new InvestmentDTO(
            Id: investment.Id,
            Name: investment.Name,
            Type: investment.Type,
            TypeDisplay: investment.Type.GetDisplayName(),
            InitialValue: investment.InitialValue.Amount,
            CurrentValue: investment.CurrentValue.Amount,
            Currency: investment.InitialValue.Currency,
            ProfitLoss: investment.ProfitLoss,
            ProfitLossPercentage: investment.ProfitLossPercentage,
            IsProfitable: investment.IsProfitable,
            PurchaseDate: investment.PurchaseDate,
            LastUpdateDate: investment.LastUpdateDate,
            UserId: investment.UserId,
            UserName: investment.User?.UserName,
            AccountId: investment.AccountId,
            AccountName: investment.Account?.Name,
            IsActive: investment.IsActive,
            CreatedAt: investment.CreatedAt,
            UpdatedAt: investment.UpdatedAt);
    }
}