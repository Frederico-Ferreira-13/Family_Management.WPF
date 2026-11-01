using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    public partial class AddEditableTransactionDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleAnchorDate",
                table: "RecurringTransactions",
                type: "TEXT",
                nullable: true);

            // Preservar a data original dos movimentos
            // que já foram gerados por recorrências.
            migrationBuilder.Sql(
                """
                UPDATE "Transactions"
                SET "ScheduledDate" = "Date"
                WHERE "RecurringTransactionId" IS NOT NULL
                  AND "ScheduledDate" IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ScheduleAnchorDate",
                table: "RecurringTransactions");
        }
    }
}