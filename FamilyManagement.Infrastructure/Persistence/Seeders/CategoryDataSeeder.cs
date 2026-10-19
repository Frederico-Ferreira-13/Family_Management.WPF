using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Model;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Persistence.Seeders;

public sealed class CategoryDataSeeder
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryDataSeeder(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var defaultCategories = new (string Name, CategoryType Type)[]
        {
            // Receitas
            ("Salário", CategoryType.Income),
            ("Trabalho independente", CategoryType.Income),
            ("Rendimentos de investimentos", CategoryType.Income),
            ("Reembolsos", CategoryType.Income),
            ("Outros rendimentos", CategoryType.Income),

            // Despesas
            ("Alimentação", CategoryType.Expense),
            ("Habitação", CategoryType.Expense),
            ("Transportes", CategoryType.Expense),
            ("Saúde", CategoryType.Expense),
            ("Educação", CategoryType.Expense),
            ("Lazer", CategoryType.Expense),
            ("Seguros", CategoryType.Expense),
            ("Serviços e subscrições", CategoryType.Expense),
            ("Impostos", CategoryType.Expense),
            ("Outras despesas", CategoryType.Expense)
        };

        var existingCategories = await _dbContext.Categories
            .Where(category =>
                category.UserId == null &&
                category.FamilyId == null)
            .Select(category => new
            {
                category.Name,
                category.Type
            })
            .ToListAsync(cancellationToken);

        foreach (var (name, type) in defaultCategories)
        {
            var exists = existingCategories.Any(category =>
                category.Type == type &&
                string.Equals(
                    category.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));

            if (exists)
                continue;

            var result = Category.Create(
                name: name,
                type: type,
                userId: null,
                familyId: null);

            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar a categoria '{name}': " +
                    result.Error.Message);
            }

            await _dbContext.Categories.AddAsync(
                result.Value,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}