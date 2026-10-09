using System;
using System.Collections.Generic;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class UserRole : BaseEntity
{
    public const string AdminName = "Admin";
    public const string MemberName = "Member";
    public const string ViewerName = "Viewer";

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private readonly List<User> _users = new();

    public virtual IReadOnlyCollection<User> Users =>
        _users.AsReadOnly();

    public bool IsBuiltIn =>
        IsBuiltInName(Name);

    private UserRole()
    {
    }

    private UserRole(string name, string? description)
    {
        Name = name;
        Description = NormalizeDescription(description);
    }

    public static Result<UserRole> Create(
        string name,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome do papel é obrigatório.");
        }

        return new UserRole(
            NormalizeName(name),
            description);
    }

    public Result UpdateDetails(
        string name,
        string? description)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "role.inactive",
                "O papel está inativo.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome do papel é obrigatório.");
        }

        var normalizedName = NormalizeName(name);

        var nameChanged = !string.Equals(
            Name,
            normalizedName,
            StringComparison.OrdinalIgnoreCase);

        if (nameChanged &&
            (IsBuiltIn || IsBuiltInName(normalizedName)))
        {
            return Error.BusinessRule(
                "role.reserved_name",
                "Não é possível alterar o nome de um papel reservado ou usar esse nome noutro papel.");
        }

        Name = normalizedName;
        Description = NormalizeDescription(description);

        Touch();

        return Result.Success();
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = name.Trim();

        if (string.Equals(
                normalizedName,
                AdminName,
                StringComparison.OrdinalIgnoreCase))
        {
            return AdminName;
        }

        if (string.Equals(
                normalizedName,
                MemberName,
                StringComparison.OrdinalIgnoreCase))
        {
            return MemberName;
        }

        if (string.Equals(
                normalizedName,
                ViewerName,
                StringComparison.OrdinalIgnoreCase))
        {
            return ViewerName;
        }

        return normalizedName;
    }

    private static string NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim();
    }

    private static bool IsBuiltInName(string name)
    {
        return string.Equals(
                   name,
                   AdminName,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   name,
                   MemberName,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   name,
                   ViewerName,
                   StringComparison.OrdinalIgnoreCase);
    }
}