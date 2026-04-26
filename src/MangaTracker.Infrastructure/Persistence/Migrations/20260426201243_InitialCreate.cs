using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MangaCollectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MalId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MalTotalVolumes = table.Column<int>(type: "int", nullable: true),
                    CustomTotalVolumes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MangaCollectionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwnedVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VolumeNumber = table.Column<int>(type: "int", nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Store = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MangaCollectionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                name: "IX_MangaCollectionItems_MalId",
                table: "MangaCollectionItems",
                column: "MalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedVolumes_MangaCollectionItemId",
                table: "OwnedVolumes",
                column: "MangaCollectionItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnedVolumes");

            migrationBuilder.DropTable(
                name: "MangaCollectionItems");
        }
    }
}
