using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Users.Mapping;

public static class UserMapper
{
    public static UserDTO ToDTO(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDTO(
            user.Id,
            user.UserName,
            user.Email.ToString(),
            user.FamilyId,
            user.Family?.Name,
            user.IsAdmin,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static UserLookupDTO ToLookupDTO(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserLookupDTO(
            user.Id,
            user.UserName,
            user.Email.ToString());
    }
}