using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAiApi.Migrations
{
    /// <inheritdoc />
    public partial class CreateLeaderboardView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE VIEW [leaderboard] AS
SELECT
    [Id] AS [id],
    [Name] AS [name],
    [Points] AS [points]
FROM [Users];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW [leaderboard];");
        }
    }
}
