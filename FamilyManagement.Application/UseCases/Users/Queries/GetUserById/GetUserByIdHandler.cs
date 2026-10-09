using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

public sealed class GetUserByIdHandler
{
    private readonly IUserRepository _repository;

    public GetUserByIdHandler(
        IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UserDTO>> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var user = await _repository
            .GetUserByIdWithFamilyAsync(query.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        return UserMapper.ToDTO(user);
    }
}