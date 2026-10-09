using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Families.Mapping;

public static class FamilyMapper
{
    public static FamilyDTO ToDTO(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);

        return new FamilyDTO(
            Id: family.Id,
            Name: family.Name,
            InvitationCode: family.InvitationCode,
            CreatorUserId: family.CreatorUserId,
            CreatorUserName: family.CreatorUser?.UserName,
            MemberCount: family.Members.Count,
            IsActive: family.IsActive,
            CreatedAt: family.CreatedAt,
            UpdatedAt: family.UpdatedAt);
    }

    public static FamilyMemberDTO ToMemberDTO(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new FamilyMemberDTO(
            Id: user.Id,
            UserName: user.UserName,
            Email: user.Email.ToString(),
            IsAdmin: user.IsAdmin,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt);
    }
}