using System;
using System.Collections.Generic;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Domain.Model;

public class User : BaseEntity
{
    public const int MaxUserNameLength = 50;

    public string UserName { get; private set; } = string.Empty;

    public EmailAddress Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    public Guid? FamilyId { get; private set; }

    public virtual Family? Family { get; private set; }

    public virtual UserSetting? UserSetting { get; private set; }

    private readonly List<UserRole> _roles = new();
    private readonly List<Account> _accounts = new();
    private readonly List<Category> _categories = new();
    private readonly List<Transaction> _transactions = new();
    private readonly List<Budget> _budgets = new();
    private readonly List<Goal> _goals = new();
    private readonly List<Investment> _investments = new();

    private readonly List<RecurringTransaction> _recurringTransactions =
        new();

    private readonly List<Family> _familiesCreated = new();

    public virtual IReadOnlyCollection<UserRole> Roles =>
        _roles.AsReadOnly();

    public virtual IReadOnlyCollection<Account> Accounts =>
        _accounts.AsReadOnly();

    public virtual IReadOnlyCollection<Category> Categories =>
        _categories.AsReadOnly();

    public virtual IReadOnlyCollection<Transaction> Transactions =>
        _transactions.AsReadOnly();

    public virtual IReadOnlyCollection<Budget> Budgets =>
        _budgets.AsReadOnly();

    public virtual IReadOnlyCollection<Goal> Goals =>
        _goals.AsReadOnly();

    public virtual IReadOnlyCollection<Investment> Investments =>
        _investments.AsReadOnly();

    public virtual IReadOnlyCollection<RecurringTransaction>
        RecurringTransactions =>
            _recurringTransactions.AsReadOnly();

    public virtual IReadOnlyCollection<Family> FamiliesCreated =>
        _familiesCreated.AsReadOnly();

    public bool HasFamily => FamilyId.HasValue;

    public bool IsAdmin => HasRole(UserRole.AdminName);

    private User()
    {
    }

    private User(
        string userName,
        EmailAddress email,
        string passwordHash)
    {
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static Result<User> Create(
        string userName,
        string email,
        string passwordHash)
    {
        var nameValidation = ValidateUserName(userName);

        if (nameValidation.IsFailure)
            return nameValidation.Error;

        var emailResult = EmailAddress.Create(email);

        if (emailResult.IsFailure)
            return emailResult.Error;

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Error.Validation(
                "O hash da palavra-passe é obrigatório.");
        }

        return new User(
            userName.Trim(),
            emailResult.Value,
            passwordHash);
    }

    public Result UpdateProfile(
        string userName,
        string email)
    {
        if (!IsActive)
            return InactiveError();

        var nameValidation = ValidateUserName(userName);

        if (nameValidation.IsFailure)
            return nameValidation;

        var emailResult = EmailAddress.Create(email);

        if (emailResult.IsFailure)
            return emailResult.Error;

        UserName = userName.Trim();
        Email = emailResult.Value;

        Touch();

        return Result.Success();
    }

    public Result UpdatePassword(string passwordHash)
    {
        if (!IsActive)
            return InactiveError();

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Error.Validation(
                "O hash da palavra-passe é obrigatório.");
        }

        PasswordHash = passwordHash;
        Touch();

        return Result.Success();
    }

    public Result AssignToFamily(Guid familyId)
    {
        if (!IsActive)
            return InactiveError();

        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (FamilyId == familyId)
            return Result.Success();

        if (FamilyId.HasValue)
        {
            return Error.BusinessRule(
                "user.already_in_family",
                "O utilizador já pertence a uma família.");
        }

        // Os papéis são atribuídos pelo serviço após a associação.
        _roles.Clear();

        FamilyId = familyId;
        Touch();

        return Result.Success();
    }

    public Result RemoveFromFamily()
    {
        if (!FamilyId.HasValue)
            return Result.Success();

        FamilyId = null;
        Family = null;

        // Não transportar permissões para uma futura família.
        _roles.Clear();

        Touch();

        return Result.Success();
    }

    public Result AssignRole(UserRole role)
    {
        if (!IsActive)
            return InactiveError();

        if (!FamilyId.HasValue)
        {
            return Error.BusinessRule(
                "user.family_required",
                "O utilizador deve pertencer a uma família antes de receber um papel.");
        }

        if (role is null || !role.IsActive)
        {
            return Error.Validation(
                "O papel deve existir e estar ativo.");
        }

        if (_roles.Any(existing => existing.Id == role.Id))
            return Result.Success();

        var sameNameExists = _roles.Any(existing =>
            string.Equals(
                existing.Name,
                role.Name,
                StringComparison.OrdinalIgnoreCase));

        if (sameNameExists)
        {
            return Error.Conflict(
                "user.duplicate_role",
                "O utilizador já tem um papel com esse nome.");
        }

        _roles.Add(role);
        Touch();

        return Result.Success();
    }

    public Result RemoveRole(Guid roleId)
    {
        if (!IsActive)
            return InactiveError();

        if (roleId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do papel é obrigatório.");
        }

        var role = _roles.FirstOrDefault(
            existing => existing.Id == roleId);

        if (role is null)
        {
            return Error.NotFound(
                "user.role_not_found",
                "O papel não está atribuído ao utilizador.");
        }

        _roles.Remove(role);
        Touch();

        return Result.Success();
    }

    public bool HasRole(string roleName)
    {
        if (!IsActive ||
            !FamilyId.HasValue ||
            string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        var normalizedName = roleName.Trim();

        return _roles.Any(role =>
            role.IsActive &&
            string.Equals(
                role.Name,
                normalizedName,
                StringComparison.OrdinalIgnoreCase));
    }

    public Result SetSettings(UserSetting settings)
    {
        if (!IsActive)
            return InactiveError();

        if (settings is null)
        {
            return Error.Validation(
                "As definições são obrigatórias.");
        }

        if (settings.UserId != Id)
        {
            return Error.Validation(
                "As definições pertencem a outro utilizador.");
        }

        if (UserSetting is not null &&
            UserSetting.Id != settings.Id)
        {
            return Error.Conflict(
                "user.settings_already_exist",
                "O utilizador já tem definições associadas.");
        }

        UserSetting = settings;
        Touch();

        return Result.Success();
    }

    private static Result ValidateUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Error.Validation(
                "O nome de utilizador é obrigatório.");
        }

        if (userName.Trim().Length > MaxUserNameLength)
        {
            return Error.Validation(
                $"O nome de utilizador não pode exceder {MaxUserNameLength} caracteres.");
        }

        return Result.Success();
    }

    private static Error InactiveError()
    {
        return Error.Conflict(
            "user.inactive",
            "O utilizador está inativo.");
    }
}