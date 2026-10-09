using FamilyManagement.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FamilyManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }


    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeUtcConverter>();

        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<NullableDateTimeUtcConverter>();
    }

    private sealed class DateTimeUtcConverter
        : ValueConverter<DateTime, DateTime>
    {
        public DateTimeUtcConverter()
            : base(
                value => value.Kind == DateTimeKind.Utc
                    ? value
                    : value.ToUniversalTime(),

                value => DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc))
        {
        }
    }

    private sealed class NullableDateTimeUtcConverter
        : ValueConverter<DateTime?, DateTime?>
    {
        public NullableDateTimeUtcConverter()
            : base(
                value => !value.HasValue
                    ? value
                    : value.Value.Kind == DateTimeKind.Utc
                        ? value
                        : value.Value.ToUniversalTime(),

                value => !value.HasValue
                    ? value
                    : DateTime.SpecifyKind(
                        value.Value,
                        DateTimeKind.Utc))
        {
        }
    }
}