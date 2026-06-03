using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReleaseHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "release_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdoWorkItemId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    AdoStateMirror = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentAssigneeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ApproverUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApproverEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_steps_release_tasks_ReleaseTaskId",
                        column: x => x.ReleaseTaskId,
                        principalTable: "release_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_events_release_tasks_ReleaseTaskId",
                        column: x => x.ReleaseTaskId,
                        principalTable: "release_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_scopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Repository = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_scopes_release_tasks_ReleaseTaskId",
                        column: x => x.ReleaseTaskId,
                        principalTable: "release_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_steps_ReleaseTaskId_StepOrder",
                table: "approval_steps",
                columns: new[] { "ReleaseTaskId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ReleaseTaskId",
                table: "audit_events",
                column: "ReleaseTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_project_scopes_ReleaseTaskId",
                table: "project_scopes",
                column: "ReleaseTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_release_tasks_AdoWorkItemId",
                table: "release_tasks",
                column: "AdoWorkItemId",
                unique: true,
                filter: "\"AdoWorkItemId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_steps");

            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "project_scopes");

            migrationBuilder.DropTable(
                name: "release_tasks");
        }
    }
}
