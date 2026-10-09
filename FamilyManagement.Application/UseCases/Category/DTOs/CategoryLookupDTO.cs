namespace FamilyManagement.Application.UseCases.Categories.DTOs;

public sealed record CategoryLookupDTO(
    Guid Id,
    string Name)
{
    public static CategoryLookupDTO None =>
        new(Guid.Empty, "Nenhuma (Categoria Pai)");

    public override string ToString() => Name;
}