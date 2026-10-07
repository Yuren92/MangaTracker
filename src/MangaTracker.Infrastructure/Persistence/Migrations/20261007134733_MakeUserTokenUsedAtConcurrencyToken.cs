using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangaTracker.Infrastructure.Persistence.Migrations
{
    // Intentionally empty: UserToken.UsedAt became a concurrency token, which changes
    // the UPDATE statements EF Core generates (WHERE UsedAt IS NULL) but not the schema.
    // The migration records the model change so the snapshot stays in sync.
    /// <inheritdoc />
    public partial class MakeUserTokenUsedAtConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
