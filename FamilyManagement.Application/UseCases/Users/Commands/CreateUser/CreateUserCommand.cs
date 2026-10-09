namespace FamilyManagement.Application.UseCases.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string Password);