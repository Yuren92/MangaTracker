using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyMalCollectionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnedVolumes");

            migrationBuilder.DropTable(
                name: "MangaCollectionItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MangaCollectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomTotalVolumes = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MalId = table.Column<int>(type: "int", nullable: false),
                    MalTotalVolumes = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaCollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MangaCollectionItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwnedVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MangaCollectionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VolumeNumber = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Store = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedVolumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnedVolumes_MangaCollectionItems_MangaCollectionItemId",
                        column: x => x.MangaCollectionItemId,
                        principalTable: "MangaCollectionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MangaCollectionItems_UserId_MalId",
                table: "MangaCollectionItems",
                columns: new[] { "UserId", "MalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedVolumes_MangaCollectionItemId",
                table: "OwnedVolumes",
                column: "MangaCollectionItemId");
        }
    }
}
