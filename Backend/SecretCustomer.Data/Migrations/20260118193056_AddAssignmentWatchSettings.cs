using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentWatchSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowSeeking",
                table: "TrainingVideoAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowSpeedChange",
                table: "TrainingVideoAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmailTemplateId",
                table: "TrainingVideoAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxWatchCount",
                table: "TrainingVideoAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinWatchCount",
                table: "TrainingVideoAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingVideoAssignments_EmailTemplateId",
                table: "TrainingVideoAssignments",
                column: "EmailTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingVideoAssignments_EmailTemplates_EmailTemplateId",
                table: "TrainingVideoAssignments",
                column: "EmailTemplateId",
                principalTable: "EmailTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingVideoAssignments_EmailTemplates_EmailTemplateId",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TrainingVideoAssignments_EmailTemplateId",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropColumn(
                name: "AllowSeeking",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropColumn(
                name: "AllowSpeedChange",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropColumn(
                name: "EmailTemplateId",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropColumn(
                name: "MaxWatchCount",
                table: "TrainingVideoAssignments");

            migrationBuilder.DropColumn(
                name: "MinWatchCount",
                table: "TrainingVideoAssignments");
        }
    }
}
