using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingVideoExternalParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingVideoExternalParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingVideoAssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    IsOpened = table.Column<bool>(type: "boolean", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WatchedSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    WatchCount = table.Column<int>(type: "integer", nullable: false),
                    FirstEmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastEmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailSentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingVideoExternalParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingVideoExternalParticipants_TrainingVideoAssignments_~",
                        column: x => x.TrainingVideoAssignmentId,
                        principalTable: "TrainingVideoAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingVideoExternalEmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingVideoExternalParticipantId = table.Column<int>(type: "integer", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentByUserId = table.Column<int>(type: "integer", nullable: true),
                    EmailTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingVideoExternalEmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingVideoExternalEmailLogs_EmailTemplates_EmailTemplate~",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingVideoExternalEmailLogs_TrainingVideoExternalPartici~",
                        column: x => x.TrainingVideoExternalParticipantId,
                        principalTable: "TrainingVideoExternalParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingVideoExternalEmailLogs_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalEmailLogs_EmailTemplateId",
                table: "TrainingVideoExternalEmailLogs",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalEmailLogs_SentAt",
                table: "TrainingVideoExternalEmailLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalEmailLogs_SentByUserId",
                table: "TrainingVideoExternalEmailLogs",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalEmailLogs_TrainingVideoExternalPartici~",
                table: "TrainingVideoExternalEmailLogs",
                column: "TrainingVideoExternalParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalParticipants_Email",
                table: "TrainingVideoExternalParticipants",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalParticipants_StatusId",
                table: "TrainingVideoExternalParticipants",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalParticipants_Token",
                table: "TrainingVideoExternalParticipants",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalParticipants_TrainingVideoAssignmentId",
                table: "TrainingVideoExternalParticipants",
                column: "TrainingVideoAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoExternalParticipants_TrainingVideoAssignmentId~",
                table: "TrainingVideoExternalParticipants",
                columns: new[] { "TrainingVideoAssignmentId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingVideoExternalEmailLogs");

            migrationBuilder.DropTable(
                name: "TrainingVideoExternalParticipants");
        }
    }
}
