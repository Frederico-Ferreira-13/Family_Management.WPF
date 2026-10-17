namespace FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;

public sealed record GetAccountsByUserQuery(
    Guid UserId,
    Guid? FamilyId = null
);