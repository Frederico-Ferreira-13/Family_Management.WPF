using FamilyManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<UserRole?> GetByNameAsync(string roleName);
    Task<IEnumerable<UserRole>> GetAllActiveRolesAsync();
}