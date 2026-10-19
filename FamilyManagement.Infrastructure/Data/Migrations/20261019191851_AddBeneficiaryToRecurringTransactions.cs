using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryToRecurringTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BeneficiaryUserId",
                table: "RecurringTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_BeneficiaryUserId",
                table: "RecurringTransactions",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_FamilyId_BeneficiaryUserId_IsActive",
                table: "RecurringTransactions",
                columns: new[] { "FamilyId", "BeneficiaryUserId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Users_BeneficiaryUserId",
                table: "RecurringTransactions",
                column: "BeneficiaryUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Users_BeneficiaryUserId",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_BeneficiaryUserId",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_FamilyId_BeneficiaryUserId_IsActive",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "BeneficiaryUserId",
                table: "RecurringTransactions");
        }
    }
}
