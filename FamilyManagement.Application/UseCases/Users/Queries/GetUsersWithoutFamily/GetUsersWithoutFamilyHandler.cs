using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.GetUsersWithoutFamily;

public sealed class GetUsersWithoutFamilyHandler
{
    private readonly IUserRepository _repository;

    public GetUsersWithoutFamilyHandler(
        IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<UserDTO>>> HandleAsync(
        GetUsersWithoutFamilyQuery query,
        CancellationToken cancellationToken = default)
    {
        var users =
            await _repository.GetUsersWithoutFamilyAsync();

        var result = users
            .Where(user => user.IsActive)
            .OrderBy(user => user.UserName)
            .Select(UserMapper.ToDTO)
            .ToList();

        return Result<IEnumerable<UserDTO>>.Success(result);
    }
}