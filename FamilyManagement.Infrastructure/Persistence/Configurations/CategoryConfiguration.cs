using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyManagement.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.UserId)
            .IsRequired(false);

        builder.Property(c => c.FamilyId)
            .IsRequired(false);

        builder.Property(c => c.ParentCategoryId)
            .IsRequired(false);

        // Category -> User
        builder.HasOne(c => c.User)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Family
        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hierarquia Category -> ParentCategory
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Budgets
        builder.HasMany(c => c.Budgets)
            .WithOne()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Transactions
        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.Category)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> RecurringTransactions
        builder.HasMany(c => c.RecurringTransactions)
            .WithOne(r => r.Category)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backing fields
        builder.Navigation(c => c.SubCategories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.Budgets)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(c => c.RecurringTransactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Evita duplicação dentro da mesma afiliação/hierarquia.
        builder.HasIndex(c => new
        {
            c.Name,
            c.UserId,
            c.FamilyId,
            c.ParentCategoryId
        })
        .IsUnique();

        builder.Ignore(c => c.IsGlobal);

        // BaseEntity
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}