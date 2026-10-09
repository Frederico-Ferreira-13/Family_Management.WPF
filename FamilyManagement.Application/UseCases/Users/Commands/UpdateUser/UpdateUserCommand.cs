namespace FamilyManagement.Application.UseCases.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string UserName,
    string Email);