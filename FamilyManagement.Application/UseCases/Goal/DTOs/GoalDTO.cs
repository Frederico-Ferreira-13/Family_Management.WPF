using System;
using System.Globalization;

namespace FamilyManagement.Application.UseCases.Goals.DTOs;

public sealed record GoalDTO(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    string Currency,
    double ProgressPercentage,
    DateTime StartDate,
    DateTime? TargetDate,
    bool IsAchieved,
    Guid UserId,
    string? UserName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsWorkingCapitalFund = false)
{
    public string TargetAmountDisplay =>
        IsWorkingCapitalFund
            ? "Sem limite"
            : TargetAmount.ToString(
                "N2",
                CultureInfo.GetCultureInfo("pt-PT"));

    public string RemainingAmountDisplay =>
        IsWorkingCapitalFund
            ? "Sem valor-alvo"
            : RemainingAmount.ToString(
                "N2",
                CultureInfo.GetCultureInfo("pt-PT"))
              + " "
              + Currency;

    public string ProgressDisplay =>
        IsWorkingCapitalFund
            ? "Reserva contínua"
            : ProgressPercentage.ToString(
                "F1",
                CultureInfo.GetCultureInfo("pt-PT"))
              + "%";
}