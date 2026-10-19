using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankNameToAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Accounts",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_BankName",
                table: "Accounts",
                column: "BankName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_BankName",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Accounts");
        }
    }
}
