using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelAssessmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelfDescription",
                table: "QuestionSubCriteria",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelfText",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentModeId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinRespondentsForAnonymity",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FeedbackRoleId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentPeriodId = table.Column<int>(type: "integer", nullable: false),
                    CustomerPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentParticipants_AssessmentParticipants_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AssessmentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentParticipants_AssignmentPeriods_AssignmentPeriodId",
                        column: x => x.AssignmentPeriodId,
                        principalTable: "AssignmentPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentParticipants_CustomerPersonnel_CustomerPersonnelId",
                        column: x => x.CustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurveyInvitationId = table.Column<int>(type: "integer", nullable: false),
                    EvaluatedCustomerPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    FeedbackRoleId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    EvaluationId = table.Column<int>(type: "integer", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentTasks_CustomerPersonnel_EvaluatedCustomerPersonne~",
                        column: x => x.EvaluatedCustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentTasks_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssessmentTasks_SurveyInvitations_SurveyInvitationId",
                        column: x => x.SurveyInvitationId,
                        principalTable: "SurveyInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParticipants_AssignmentPeriodId",
                table: "AssessmentParticipants",
                column: "AssignmentPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParticipants_AssignmentPeriodId_CustomerPersonnel~",
                table: "AssessmentParticipants",
                columns: new[] { "AssignmentPeriodId", "CustomerPersonnelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParticipants_CustomerPersonnelId",
                table: "AssessmentParticipants",
                column: "CustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentParticipants_ParentId",
                table: "AssessmentParticipants",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTasks_EvaluatedCustomerPersonnelId",
                table: "AssessmentTasks",
                column: "EvaluatedCustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTasks_EvaluationId",
                table: "AssessmentTasks",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTasks_SurveyInvitationId",
                table: "AssessmentTasks",
                column: "SurveyInvitationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentParticipants");

            migrationBuilder.DropTable(
                name: "AssessmentTasks");

            migrationBuilder.DropColumn(
                name: "SelfDescription",
                table: "QuestionSubCriteria");

            migrationBuilder.DropColumn(
                name: "SelfText",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AssessmentModeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "MinRespondentsForAnonymity",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FeedbackRoleId",
                table: "Evaluations");
        }
    }
}
