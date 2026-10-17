using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkTransactionsToFinancialPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FinancialPlanId",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinancialPlanId",
                table: "RecurringTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FinancialPlanId_Date",
                table: "Transactions",
                columns: new[] { "FinancialPlanId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_FinancialPlanId_NextDueDate",
                table: "RecurringTransactions",
                columns: new[] { "FinancialPlanId", "NextDueDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_FinancialPlans_FinancialPlanId",
                table: "RecurringTransactions",
                column: "FinancialPlanId",
                principalTable: "FinancialPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FinancialPlans_FinancialPlanId",
                table: "Transactions",
                column: "FinancialPlanId",
                principalTable: "FinancialPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_FinancialPlans_FinancialPlanId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FinancialPlans_FinancialPlanId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FinancialPlanId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_FinancialPlanId_NextDueDate",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "FinancialPlanId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FinancialPlanId",
                table: "RecurringTransactions");
        }
    }
}
