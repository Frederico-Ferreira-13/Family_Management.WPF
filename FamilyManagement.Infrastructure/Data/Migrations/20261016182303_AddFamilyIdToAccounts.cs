using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyIdToAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_CategoryId_Month_Year",
                table: "Budgets");

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Transactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Goals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Budgets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoalContributionImpacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevertedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalContributionImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalContributionImpacts_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoalContributionImpacts_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FamilyId_Date",
                table: "Transactions",
                columns: new[] { "FamilyId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_FamilyId",
                table: "Goals",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_FamilyId_IsActive",
                table: "Goals",
                columns: new[] { "FamilyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FamilyId_Year_Month",
                table: "Budgets",
                columns: new[] { "FamilyId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "UX_Budgets_Family_Period",
                table: "Budgets",
                columns: new[] { "FamilyId", "CategoryId", "Month", "Year" },
                unique: true,
                filter: "\"FamilyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Budgets_Personal_Period",
                table: "Budgets",
                columns: new[] { "UserId", "CategoryId", "Month", "Year" },
                unique: true,
                filter: "\"FamilyId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GoalContributionImpacts_GoalId_IsApplied",
                table: "GoalContributionImpacts",
                columns: new[] { "GoalId", "IsApplied" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalContributionImpacts_TransactionId",
                table: "GoalContributionImpacts",
                column: "TransactionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Budgets_Families_FamilyId",
                table: "Budgets",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Families_FamilyId",
                table: "Goals",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Families_FamilyId",
                table: "Transactions",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budgets_Families_FamilyId",
                table: "Budgets");

            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Families_FamilyId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Families_FamilyId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "GoalContributionImpacts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FamilyId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Goals_FamilyId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_FamilyId_IsActive",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_FamilyId_Year_Month",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "UX_Budgets_Family_Period",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "UX_Budgets_Personal_Period",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_CategoryId_Month_Year",
                table: "Budgets",
                columns: new[] { "UserId", "CategoryId", "Month", "Year" },
                unique: true);
        }
    }
}
