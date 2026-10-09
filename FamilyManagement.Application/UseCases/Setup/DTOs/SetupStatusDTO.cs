using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Application.UseCases.Setup.DTOs;

public sealed record SetupStatusDTO
{
    public Guid FamilyId { get; init; }

    public bool HasUsers { get; init; }
    public bool HasAccounts { get; init; }
    public bool HasCategories { get; init; }
    public bool HasGoals { get; init; }

    public bool IsComplete { get; init; }

    public double CompletionPercentage { get; init; }

    public bool IsReadyToUse =>
        HasAccounts &&
        HasCategories;
}