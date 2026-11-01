using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyManagement.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingCapitalFund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkingCapitalKey",
                table: "Goals",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_WorkingCapitalKey",
                table: "Goals",
                column: "WorkingCapitalKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Goals_WorkingCapitalKey",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "WorkingCapitalKey",
                table: "Goals");
        }
    }
}
