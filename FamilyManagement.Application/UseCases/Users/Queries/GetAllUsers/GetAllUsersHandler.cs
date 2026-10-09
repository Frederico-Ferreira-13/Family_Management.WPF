using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.GetAllUsers;

public sealed class GetAllUsersHandler
{
    private readonly IUserRepository _repository;

    public GetAllUsersHandler(
        IUserRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(
                nameof(repository));
    }

    public async Task<Result<IEnumerable<UserDTO>>> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var users =
            await _repository
                .GetAllUsersWithDetailsAsync();

        cancellationToken.ThrowIfCancellationRequested();

        var result = users
            .OrderBy(user => user.UserName)
            .Select(UserMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<UserDTO>>
            .Success(result);
    }
}