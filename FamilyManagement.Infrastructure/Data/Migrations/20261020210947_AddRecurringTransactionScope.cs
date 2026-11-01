using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactionScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar Scope:
            // 1 = Personal
            // 2 = Family

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "RecurringTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Preservar a classificação das recorrências existentes.
            // FamilyId preenchido -> Family (2)
            // FamilyId nulo       -> Personal (1)

            migrationBuilder.Sql(
                """
                UPDATE "RecurringTransactions"
                SET "Scope" = CASE
                    WHEN "FamilyId" IS NOT NULL THEN 2
                    ELSE 1
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringTransactions");
        }
    }
}