using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingVideoEmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailSentCount",
                table: "TrainingVideoParticipants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstEmailSentAt",
                table: "TrainingVideoParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailSentAt",
                table: "TrainingVideoParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingVideoEmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingVideoParticipantId = table.Column<int>(type: "integer", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentByUserId = table.Column<int>(type: "integer", nullable: true),
                    EmailTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingVideoEmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingVideoEmailLogs_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainingVideoEmailLogs_TrainingVideoParticipants_TrainingVi~",
                        column: x => x.TrainingVideoParticipantId,
                        principalTable: "TrainingVideoParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingVideoEmailLogs_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoEmailLogs_EmailTemplateId",
                table: "TrainingVideoEmailLogs",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoEmailLogs_SentByUserId",
                table: "TrainingVideoEmailLogs",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoEmailLogs_TrainingVideoParticipantId",
                table: "TrainingVideoEmailLogs",
                column: "TrainingVideoParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingVideoEmailLogs");

            migrationBuilder.DropColumn(
                name: "EmailSentCount",
                table: "TrainingVideoParticipants");

            migrationBuilder.DropColumn(
                name: "FirstEmailSentAt",
                table: "TrainingVideoParticipants");

            migrationBuilder.DropColumn(
                name: "LastEmailSentAt",
                table: "TrainingVideoParticipants");
        }
    }
}
