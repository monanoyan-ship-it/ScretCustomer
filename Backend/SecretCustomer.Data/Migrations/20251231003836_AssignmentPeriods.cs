using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentPeriodId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    AverageScore = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentPeriods_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentPeriods_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_AssignmentPeriodId",
                table: "Evaluations",
                column: "AssignmentPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentPeriods_AssignmentId",
                table: "AssignmentPeriods",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentPeriods_CreatedByUserId",
                table: "AssignmentPeriods",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_AssignmentPeriods_AssignmentPeriodId",
                table: "Evaluations",
                column: "AssignmentPeriodId",
                principalTable: "AssignmentPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_AssignmentPeriods_AssignmentPeriodId",
                table: "Evaluations");

            migrationBuilder.DropTable(
                name: "AssignmentPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_AssignmentPeriodId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "AssignmentPeriodId",
                table: "Evaluations");
        }
    }
}
