using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Goals.Queries.GetGoalsByFamily;

public sealed class GetGoalsByFamilyHandler
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUserRepository _userRepository;

    public GetGoalsByFamilyHandler(
        IGoalRepository goalRepository,
        IUserRepository userRepository)
    {
        _goalRepository = goalRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<GoalDTO>> HandleAsync(
        GetGoalsByFamilyQuery query)
    {
        if (query.FamilyId == Guid.Empty)
            return Array.Empty<GoalDTO>();

        var familyMembers =
            await _userRepository.GetUsersByFamilyIdAsync(
                query.FamilyId);

        var userIds = familyMembers
            .Select(user => user.Id)
            .ToHashSet();

        if (userIds.Count == 0)
            return Array.Empty<GoalDTO>();

        var goals =
            await _goalRepository.FindGoalsWithDetailsAsync(
                goal => userIds.Contains(goal.UserId));

        return goals
            .Select(GoalMapper.ToDTO)
            .ToList();
    }
}