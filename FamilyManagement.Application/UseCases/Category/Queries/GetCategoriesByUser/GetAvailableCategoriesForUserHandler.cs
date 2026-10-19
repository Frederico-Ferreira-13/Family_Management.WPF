using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;

public sealed class GetAvailableCategoriesForUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAvailableCategoriesForUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IReadOnlyCollection<CategoryDTO>>> HandleAsync(
        GetAvailableCategoriesForUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O utilizador não existe ou está inativo.");
        }

        // Mantém a compatibilidade com chamadas anteriores.
        // Quando a família é indicada explicitamente,
        // utilizamos essa família como contexto da consulta.
        var familyId = query.FamilyId ?? user.FamilyId;

        // Uma família explicitamente indicada deve pertencer
        // ao contexto autorizado do utilizador.
        // A verificação abaixo assume o modelo atual de
        // associação User.FamilyId.
        if (query.FamilyId.HasValue &&
            user.FamilyId != query.FamilyId)
        {
            return Error.Validation(
                "O utilizador não está associado à família indicada.");
        }

        var categories = await _unitOfWork.Categories.FindAsync(
            category =>
                category.IsActive &&
                (
                    category.UserId == query.UserId ||
                    (
                        familyId.HasValue &&
                        category.FamilyId == familyId.Value
                    ) ||
                    (
                        !category.UserId.HasValue &&
                        !category.FamilyId.HasValue
                    )
                ));

        cancellationToken.ThrowIfCancellationRequested();

        var result = categories
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category =>
                CategoryMapper.ToDTO(
                    category,
                    includeSubCategories: false))
            .ToList();

        return result;
    }
}