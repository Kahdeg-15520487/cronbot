using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CronBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false, defaultValue: "Info"),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GitCommit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GitBranch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FilesAffected = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_logs_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_logs_CreatedAt",
                table: "task_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_task_logs_TaskId",
                table: "task_logs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_task_logs_Type",
                table: "task_logs",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_logs");
        }
    }
}
