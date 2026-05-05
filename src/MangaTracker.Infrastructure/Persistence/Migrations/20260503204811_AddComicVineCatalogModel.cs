using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComicVineCatalogModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OriginalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Editions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComicVineVolumeId = table.Column<int>(type: "int", nullable: false),
                    ComicVineApiDetailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PublisherName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartYear = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SiteDetailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssueCount = table.Column<int>(type: "int", nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Editions_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComicVineIssueId = table.Column<int>(type: "int", nullable: false),
                    ComicVineApiDetailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IssueNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedNumber = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StoreDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SiteDetailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tomes_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCollections_Editions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "Editions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCollections_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOwnedTomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserCollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOwnedTomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOwnedTomes_Tomes_TomeId",
                        column: x => x.TomeId,
                        principalTable: "Tomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOwnedTomes_UserCollections_UserCollectionId",
                        column: x => x.UserCollectionId,
                        principalTable: "UserCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Editions_ComicVineApiDetailUrl",
                table: "Editions",
                column: "ComicVineApiDetailUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Editions_ComicVineVolumeId",
                table: "Editions",
                column: "ComicVineVolumeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Editions_SeriesId",
                table: "Editions",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Tomes_ComicVineApiDetailUrl",
                table: "Tomes",
                column: "ComicVineApiDetailUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tomes_ComicVineIssueId",
                table: "Tomes",
                column: "ComicVineIssueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tomes_EditionId_IssueNumber",
                table: "Tomes",
                columns: new[] { "EditionId", "IssueNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCollections_EditionId",
                table: "UserCollections",
                column: "EditionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollections_UserId_EditionId",
                table: "UserCollections",
                columns: new[] { "UserId", "EditionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOwnedTomes_TomeId",
                table: "UserOwnedTomes",
                column: "TomeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOwnedTomes_UserCollectionId_TomeId",
                table: "UserOwnedTomes",
                columns: new[] { "UserCollectionId", "TomeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOwnedTomes");

            migrationBuilder.DropTable(
                name: "Tomes");

            migrationBuilder.DropTable(
                name: "UserCollections");

            migrationBuilder.DropTable(
                name: "Editions");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
