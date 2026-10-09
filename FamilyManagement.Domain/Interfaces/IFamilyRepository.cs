using FamilyManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface IFamilyRepository : IRepository<Family>
{
    Task<Family?> GetFamilyByNameAsync(string name);
    Task<Family?> GetFamilyByInvitationCodeAsync(string code);

    Task<Family?> GetByIdWithDetailsAsync(Guid familyId);
    Task<IEnumerable<Family>> FindFamiliesWithDetailsAsync(Expression<Func<Family, bool>> predicate);

    Task<IEnumerable<Family>> GetAllActiveFamiliesAsync();

    Task<bool> IsUserMemberOfFamilyAsync(Guid familyId, Guid userId);
    Task<bool> IsUserCreatorOfFamilyAsync(Guid familyId, Guid userId);
    Task<bool> FamilyNameExistsAsync(string name, Guid? excludeFamilyId = null);
}