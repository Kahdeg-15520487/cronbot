using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CronBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TelegramId = table.Column<long>(type: "bigint", nullable: true),
                    AuthentikId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPersonal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Settings = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teams_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GitMode = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExternalGitUrl = table.Column<string>(type: "text", nullable: true),
                    ExternalGitTokenEncrypted = table.Column<string>(type: "text", nullable: true),
                    InternalRepoId = table.Column<int>(type: "integer", nullable: true),
                    InternalRepoUrl = table.Column<string>(type: "text", nullable: true),
                    AutonomyLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SandboxMode = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TemplateName = table.Column<string>(type: "text", nullable: true),
                    KomodoStackId = table.Column<string>(type: "text", nullable: true),
                    MaxAgents = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    Settings = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    ScalingConfig = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    ErrorHandlingConfig = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                    table.UniqueConstraint("AK_projects_TeamId_Slug", x => new { x.TeamId, x.Slug });
                    table.ForeignKey(
                        name: "FK_projects_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => new { x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_team_members_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Columns = table.Column<string>(type: "text", nullable: false, defaultValue: "[\"Backlog\", \"Sprint\", \"In Progress\", \"Review\", \"Blocked\", \"Done\"]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boards_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mcps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "1.0.0"),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GroupType = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContainerImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KomodoStackId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Transport = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    Command = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnvironmentVars = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    RequiredConfig = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Tools = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Resources = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Prompts = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StatusMessage = table.Column<string>(type: "text", nullable: true),
                    LastHealthCheck = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mcps_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mcps_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    Channel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_project_members_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "1.0.0"),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Meta = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skills_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_skills_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VelocityPoints = table.Column<int>(type: "integer", nullable: true),
                    RetrospectiveNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.Id);
                    table.UniqueConstraint("AK_sprints_ProjectId_Number", x => new { x.ProjectId, x.Number });
                    table.ForeignKey(
                        name: "FK_sprints_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: true),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StoryPoints = table.Column<int>(type: "integer", nullable: true),
                    AssigneeType = table.Column<string>(type: "text", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    GitBranch = table.Column<string>(type: "text", nullable: true),
                    GitPrId = table.Column<int>(type: "integer", nullable: true),
                    GitPrUrl = table.Column<string>(type: "text", nullable: true),
                    GitMergedCommit = table.Column<string>(type: "text", nullable: true),
                    BlockedByTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockedReason = table.Column<string>(type: "text", nullable: true),
                    ColumnOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                    table.UniqueConstraint("AK_tasks_ProjectId_Number", x => new { x.ProjectId, x.Number });
                    table.ForeignKey(
                        name: "FK_tasks_boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tasks_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tasks_tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContainerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContainerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StatusMessage = table.Column<string>(type: "text", nullable: true),
                    CpuUsagePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    MemoryUsageMb = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TerminatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TasksCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CommitsMade = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agents_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agents_tasks_CurrentTaskId",
                        column: x => x.CurrentTaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "task_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_comments_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agents_CurrentTaskId",
                table: "agents",
                column: "CurrentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_agents_ProjectId",
                table: "agents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_agents_Status",
                table: "agents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_boards_ProjectId",
                table: "boards",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_mcps_GroupType",
                table: "mcps",
                column: "GroupType");

            migrationBuilder.CreateIndex(
                name: "IX_mcps_ProjectId",
                table: "mcps",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_mcps_TeamId",
                table: "mcps",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                column: "UserId",
                filter: "\"ReadAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAt",
                table: "notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ProjectId",
                table: "notifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_project_members_UserId",
                table: "project_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_Slug",
                table: "projects",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_projects_TeamId",
                table: "projects",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_ProjectId",
                table: "skills",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_Scope",
                table: "skills",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_skills_TeamId",
                table: "skills",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_ProjectId",
                table: "sprints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_Status",
                table: "sprints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_task_comments_TaskId",
                table: "task_comments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_AssigneeType_AssigneeId",
                table: "tasks",
                columns: new[] { "AssigneeType", "AssigneeId" });

            migrationBuilder.CreateIndex(
                name: "IX_tasks_BoardId",
                table: "tasks",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_ParentTaskId",
                table: "tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_ProjectId",
                table: "tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_SprintId",
                table: "tasks",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_Status",
                table: "tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_UserId",
                table: "team_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_OwnerId",
                table: "teams",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_Slug",
                table: "teams",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_TelegramId",
                table: "users",
                column: "TelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "mcps");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "task_comments");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "boards");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
