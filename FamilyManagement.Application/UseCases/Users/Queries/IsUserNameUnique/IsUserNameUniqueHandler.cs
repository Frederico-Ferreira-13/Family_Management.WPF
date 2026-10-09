using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.IsUserNameUnique;

public sealed class IsUserNameUniqueHandler
{
    private readonly IUserRepository _repository;

    public IsUserNameUniqueHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> HandleAsync(
        IsUserNameUniqueQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.UserName))
            return Error.Validation(
                "O nome de utilizador é obrigatório.");

        var exists = await _repository
            .ExistsByUserNameAsync(query.UserName);

        return Result<bool>.Success(!exists);
    }
}