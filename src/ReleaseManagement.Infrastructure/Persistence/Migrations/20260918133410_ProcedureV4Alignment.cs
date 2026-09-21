using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReleaseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProcedureV4Alignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualWindowEnd",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualWindowStart",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassificationCriteria",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DirectorApprovalReference",
                table: "Releases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionMode",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExpeditedJustification",
                table: "Releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ForecastId",
                table: "Releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyDependencies",
                table: "Releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceApprovalReference",
                table: "Releases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OperationalImpact",
                table: "Releases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Outcome",
                table: "Releases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeNotes",
                table: "Releases",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlannedMaintenance",
                table: "Releases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedWindowEnd",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedWindowStart",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RecoveryApproach",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryDecisionPoints",
                table: "Releases",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecoveryResponsibleParties",
                table: "Releases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseManagerUserId",
                table: "Releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityTriggers",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StabilizationEnd",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StabilizationNotes",
                table: "Releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StabilizationStart",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalOwnerUserId",
                table: "Releases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Track",
                table: "Releases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FreezePeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FreezeType = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Authority = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreezePeriods_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostImplementationReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Triggers = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    RootCause = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    LessonsLearned = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    BacklogReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostImplementationReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostImplementationReviews_ApplicationUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostImplementationReviews_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostImplementationReviews_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostReleaseValidations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TechnicalResult = table.Column<int>(type: "integer", nullable: false),
                    HealthCheckPassed = table.Column<bool>(type: "boolean", nullable: false),
                    SmokeTestPassed = table.Column<bool>(type: "boolean", nullable: false),
                    MonitoringClean = table.Column<bool>(type: "boolean", nullable: false),
                    RecoveryNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    TechnicalNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TechnicalEvidenceReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TechnicalValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicalValidatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BusinessValidationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    BusinessResult = table.Column<int>(type: "integer", nullable: false),
                    BusinessNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BusinessValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessValidatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostReleaseValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostReleaseValidations_ApplicationUsers_BusinessValidatedBy~",
                        column: x => x.BusinessValidatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostReleaseValidations_ApplicationUsers_TechnicalValidatedB~",
                        column: x => x.TechnicalValidatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostReleaseValidations_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlType = table.Column<int>(type: "integer", nullable: false),
                    OwnerRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EvidenceReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Justification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessControls_ApplicationUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReadinessControls_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseCommunications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunicationType = table.Column<int>(type: "integer", nullable: false),
                    Audience = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Channel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseCommunications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseCommunications_ApplicationUsers_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseCommunications_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Quarter = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Dependencies = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseForecasts_ApplicationUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseForecasts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseForecasts_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseReferences_ApplicationUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleaseReferences_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreezeExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FreezePeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Justification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezeExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreezeExceptions_ApplicationUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FreezeExceptions_FreezePeriods_FreezePeriodId",
                        column: x => x.FreezePeriodId,
                        principalTable: "FreezePeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FreezeExceptions_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PirActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostImplementationReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PirActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PirActions_PostImplementationReviews_PostImplementationRevi~",
                        column: x => x.PostImplementationReviewId,
                        principalTable: "PostImplementationReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_Category",
                table: "Releases",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_ForecastId",
                table: "Releases",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_ReleaseManagerUserId",
                table: "Releases",
                column: "ReleaseManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_TechnicalOwnerUserId",
                table: "Releases",
                column: "TechnicalOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreezeExceptions_FreezePeriodId_ReleaseId",
                table: "FreezeExceptions",
                columns: new[] { "FreezePeriodId", "ReleaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreezeExceptions_RecordedByUserId",
                table: "FreezeExceptions",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreezeExceptions_ReleaseId",
                table: "FreezeExceptions",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_FreezePeriods_CreatedByUserId",
                table: "FreezePeriods",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreezePeriods_IsActive_StartDate_EndDate",
                table: "FreezePeriods",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PirActions_PostImplementationReviewId",
                table: "PirActions",
                column: "PostImplementationReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PostImplementationReviews_CompletedByUserId",
                table: "PostImplementationReviews",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostImplementationReviews_CreatedByUserId",
                table: "PostImplementationReviews",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostImplementationReviews_ReleaseId",
                table: "PostImplementationReviews",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostImplementationReviews_Status",
                table: "PostImplementationReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PostReleaseValidations_BusinessValidatedByUserId",
                table: "PostReleaseValidations",
                column: "BusinessValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostReleaseValidations_ReleaseId",
                table: "PostReleaseValidations",
                column: "ReleaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostReleaseValidations_TechnicalValidatedByUserId",
                table: "PostReleaseValidations",
                column: "TechnicalValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessControls_ReleaseId_ControlType",
                table: "ReadinessControls",
                columns: new[] { "ReleaseId", "ControlType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessControls_Status_IsRequired",
                table: "ReadinessControls",
                columns: new[] { "Status", "IsRequired" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessControls_UpdatedByUserId",
                table: "ReadinessControls",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseCommunications_ReleaseId_SentDate",
                table: "ReleaseCommunications",
                columns: new[] { "ReleaseId", "SentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseCommunications_SentByUserId",
                table: "ReleaseCommunications",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseForecasts_CreatedByUserId",
                table: "ReleaseForecasts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseForecasts_ProductId",
                table: "ReleaseForecasts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseForecasts_ReleaseId",
                table: "ReleaseForecasts",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseForecasts_Year_Quarter",
                table: "ReleaseForecasts",
                columns: new[] { "Year", "Quarter" });

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseReferences_AddedByUserId",
                table: "ReleaseReferences",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseReferences_ReleaseId_ReferenceType",
                table: "ReleaseReferences",
                columns: new[] { "ReleaseId", "ReferenceType" });

            migrationBuilder.AddForeignKey(
                name: "FK_Releases_ApplicationUsers_ReleaseManagerUserId",
                table: "Releases",
                column: "ReleaseManagerUserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Releases_ApplicationUsers_TechnicalOwnerUserId",
                table: "Releases",
                column: "TechnicalOwnerUserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Releases_ApplicationUsers_ReleaseManagerUserId",
                table: "Releases");

            migrationBuilder.DropForeignKey(
                name: "FK_Releases_ApplicationUsers_TechnicalOwnerUserId",
                table: "Releases");

            migrationBuilder.DropTable(
                name: "FreezeExceptions");

            migrationBuilder.DropTable(
                name: "PirActions");

            migrationBuilder.DropTable(
                name: "PostReleaseValidations");

            migrationBuilder.DropTable(
                name: "ReadinessControls");

            migrationBuilder.DropTable(
                name: "ReleaseCommunications");

            migrationBuilder.DropTable(
                name: "ReleaseForecasts");

            migrationBuilder.DropTable(
                name: "ReleaseReferences");

            migrationBuilder.DropTable(
                name: "FreezePeriods");

            migrationBuilder.DropTable(
                name: "PostImplementationReviews");

            migrationBuilder.DropIndex(
                name: "IX_Releases_Category",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Releases_ForecastId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Releases_ReleaseManagerUserId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Releases_TechnicalOwnerUserId",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ActualWindowEnd",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ActualWindowStart",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ClassificationCriteria",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "DirectorApprovalReference",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ExecutionMode",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ExpeditedJustification",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ForecastId",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "KeyDependencies",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "MaintenanceApprovalReference",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "OperationalImpact",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "OutcomeNotes",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "PlannedMaintenance",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "PlannedWindowEnd",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "PlannedWindowStart",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "RecoveryApproach",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "RecoveryDecisionPoints",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "RecoveryResponsibleParties",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "ReleaseManagerUserId",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "SecurityTriggers",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "StabilizationEnd",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "StabilizationNotes",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "StabilizationStart",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "TechnicalOwnerUserId",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "Track",
                table: "Releases");
        }
    }
}
