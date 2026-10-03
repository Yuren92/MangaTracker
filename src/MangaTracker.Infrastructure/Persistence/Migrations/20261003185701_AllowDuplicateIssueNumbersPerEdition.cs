using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateIssueNumbersPerEdition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tomes_EditionId_IssueNumber",
                table: "Tomes");

            migrationBuilder.CreateIndex(
                name: "IX_Tomes_EditionId_IssueNumber",
                table: "Tomes",
                columns: new[] { "EditionId", "IssueNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tomes_EditionId_IssueNumber",
                table: "Tomes");

            migrationBuilder.CreateIndex(
                name: "IX_Tomes_EditionId_IssueNumber",
                table: "Tomes",
                columns: new[] { "EditionId", "IssueNumber" },
                unique: true);
        }
    }
}
