using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.UserExistsByEmail;

public sealed class UserExistsByEmailHandler
{
    private readonly IUserRepository _repository;

    public UserExistsByEmailHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> HandleAsync(
        UserExistsByEmailQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
            return Error.Validation("O email é obrigatório.");

        var exists = await _repository
            .ExistsByEmailAsync(query.Email);

        return Result<bool>.Success(exists);
    }
}