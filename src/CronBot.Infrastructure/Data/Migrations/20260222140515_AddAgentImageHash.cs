using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CronBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentImageHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                table: "agents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageHash",
                table: "agents");
        }
    }
}
