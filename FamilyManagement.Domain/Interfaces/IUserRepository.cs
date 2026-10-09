using FamilyManagement.Domain.Model;

namespace FamilyManagement.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUserNameAsync(string userName);

    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUserNameAsync(string userName);

    Task<IEnumerable<User>> GetAllActiveUsersAsync();

    // Gestão administrativa.
    Task<IEnumerable<User>> GetAllUsersWithDetailsAsync();

    Task<bool> UserExistsAsync(Guid id);

    Task<IEnumerable<User>> GetUsersByFamilyIdAsync(Guid familyId);
    Task<IEnumerable<User>> GetUsersWithoutFamilyAsync();

    Task<int> CountMembersInFamilyAsync(Guid familyId);

    Task<IEnumerable<User>> GetFamilyMembersWithDetailsAsync(
        Guid familyId);

    Task<User?> GetUserByIdWithSettingsAsync(Guid userId);

    Task<User?> GetUserByIdWithFamilyAsync(Guid id);

    Task<User?> GetUserByIdWithSettingsAndFamilyAsync(
        Guid userId);

    Task<User?> GetUserByIdWithFamilyAndRolesAsync(
        Guid userId);
}