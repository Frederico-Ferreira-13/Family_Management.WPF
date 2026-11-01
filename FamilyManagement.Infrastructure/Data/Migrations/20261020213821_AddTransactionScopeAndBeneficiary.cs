using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionScopeAndBeneficiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_FamilyId_Date",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "BeneficiaryUserId",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BeneficiaryUserId",
                table: "Transactions",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BeneficiaryUserId_Date",
                table: "Transactions",
                columns: new[] { "BeneficiaryUserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FamilyId_Scope_Date",
                table: "Transactions",
                columns: new[] { "FamilyId", "Scope", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions",
                column: "Scope");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_BeneficiaryUserId",
                table: "Transactions",
                column: "BeneficiaryUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_BeneficiaryUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BeneficiaryUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BeneficiaryUserId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FamilyId_Scope_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FamilyId_Date",
                table: "Transactions",
                columns: new[] { "FamilyId", "Date" });
        }
    }
}
