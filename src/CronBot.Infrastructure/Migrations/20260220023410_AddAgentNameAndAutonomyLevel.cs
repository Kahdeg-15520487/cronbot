using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CronBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentNameAndAutonomyLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutonomyLevel",
                table: "agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "agents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "agents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutonomyLevel",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "agents");
        }
    }
}
