using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedChecklistFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupType",
                table: "Sections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Sections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPoints",
                table: "Sections",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPoints",
                table: "Sections",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HelpText",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPoints",
                table: "Questions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyType",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyValue",
                table: "Questions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedNote",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoringType",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPoints",
                table: "Questions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CallDate",
                table: "Evaluations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallId",
                table: "Evaluations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ControlDate",
                table: "Evaluations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ControlTime",
                table: "Evaluations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvaluatedPersonnelId",
                table: "Evaluations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluatedUnknownPersonnel",
                table: "Evaluations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationComment",
                table: "Evaluations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FormOpenedAt",
                table: "Evaluations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RedCardCount",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YellowCardCount",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChecklistType",
                table: "Checklists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Checklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxTotalPoints",
                table: "Checklists",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScoringMethod",
                table: "Checklists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TemplateName",
                table: "Checklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Checklists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Checklists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedPenaltyType",
                table: "Answers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GivenPoints",
                table: "Answers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPenaltyApplied",
                table: "Answers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationNotes",
                table: "Answers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupType",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "WeightPoints",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "HelpText",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PenaltyType",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "PenaltyValue",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "RecommendedNote",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ScoringType",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "WeightPoints",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CallDate",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "CallId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "ControlDate",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "ControlTime",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "EvaluatedPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "EvaluatedUnknownPersonnel",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "EvaluationComment",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "FormOpenedAt",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "RedCardCount",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "YellowCardCount",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "ChecklistType",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "MaxTotalPoints",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "ScoringMethod",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "TemplateName",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "AppliedPenaltyType",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "GivenPoints",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "IsPenaltyApplied",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "RecommendationNotes",
                table: "Answers");
        }
    }
}
