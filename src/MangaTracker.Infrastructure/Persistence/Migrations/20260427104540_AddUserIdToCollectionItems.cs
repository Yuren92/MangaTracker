using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToCollectionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MangaCollectionItems_MalId",
                table: "MangaCollectionItems");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MangaCollectionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MangaCollectionItems_UserId_MalId",
                table: "MangaCollectionItems",
                columns: new[] { "UserId", "MalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MangaCollectionItems_UserId_MalId",
                table: "MangaCollectionItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MangaCollectionItems");

            migrationBuilder.CreateIndex(
                name: "IX_MangaCollectionItems_MalId",
                table: "MangaCollectionItems",
                column: "MalId",
                unique: true);
        }
    }
}
