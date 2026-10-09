using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.GetUsersByFamily;

public sealed class GetUsersByFamilyHandler
{
    private readonly IUserRepository _repository;

    public GetUsersByFamilyHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<UserDTO>>> HandleAsync(
        GetUsersByFamilyQuery query)
    {
        var users = await _repository
            .GetFamilyMembersWithDetailsAsync(query.FamilyId);

        return Result<IEnumerable<UserDTO>>.Success(
            users.Select(UserMapper.ToDTO).ToList());
    }
}