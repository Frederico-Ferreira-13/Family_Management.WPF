using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class Family : BaseEntity
{
    public const int InvitationCodeLength = 32;

    public string Name { get; private set; } = string.Empty;

    public Guid CreatorUserId { get; private set; }

    public string InvitationCode { get; private set; } =
        string.Empty;

    public virtual User? CreatorUser { get; private set; }

    private readonly List<User> _members = new();

    public virtual IReadOnlyCollection<User> Members =>
        _members.AsReadOnly();

    private Family()
    {
    }

    private Family(string name, Guid creatorUserId)
    {
        Name = name;
        CreatorUserId = creatorUserId;

        InvitationCode = GenerateInvitationCode();
    }

    public static Result<Family> Create(
        string name,
        Guid creatorUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome da família é obrigatório.");
        }

        if (creatorUserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador criador é obrigatório.");
        }

        return new Family(
            name.Trim(),
            creatorUserId);
    }

    public Result Rename(string name)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "family.inactive",
                "A família está inativa.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome da família é obrigatório.");
        }

        var normalizedName = name.Trim();

        if (Name == normalizedName)
            return Result.Success();

        Name = normalizedName;
        Touch();

        return Result.Success();
    }

    public Result RegenerateInvitationCode()
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "family.inactive",
                "Não é possível gerar um convite para uma família inativa.");
        }

        InvitationCode = GenerateInvitationCode();
        Touch();

        return Result.Success();
    }

    public Result SetInvitationCode(string code)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "family.inactive",
                "Não é possível alterar o convite de uma família inativa.");
        }

        var normalizedCode = code?
            .Trim()
            .ToUpperInvariant();

        if (normalizedCode is null ||
            normalizedCode.Length != InvitationCodeLength ||
            !normalizedCode.All(IsHexadecimalCharacter))
        {
            return Error.Validation(
                "O código de convite deve conter 32 caracteres hexadecimais.");
        }

        if (InvitationCode == normalizedCode)
            return Result.Success();

        InvitationCode = normalizedCode;
        Touch();

        return Result.Success();
    }

    private static string GenerateInvitationCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);

        return Convert.ToHexString(bytes);
    }

    private static bool IsHexadecimalCharacter(char character)
    {
        return character is >= '0' and <= '9'
            or >= 'A' and <= 'F';
    }
}