namespace FamilyManagement.Application.UseCases.Authentication.Commands.Login;

public sealed record LoginCommand(
    string EmailOrUserName,
    string Password);