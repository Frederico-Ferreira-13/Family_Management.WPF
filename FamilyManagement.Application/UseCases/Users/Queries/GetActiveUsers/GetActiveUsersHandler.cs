using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.GetActiveUsers;

public sealed class GetActiveUsersHandler
{
    private readonly IUserRepository _repository;

    public GetActiveUsersHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<UserDTO>>> HandleAsync(
        GetActiveUsersQuery query)
    {
        var users = await _repository.GetAllActiveUsersAsync();

        return Result<IEnumerable<UserDTO>>.Success(
            users.Select(UserMapper.ToDTO).ToList());
    }
}