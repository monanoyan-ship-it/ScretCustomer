using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentPeriodProjectIdAndNullableAssignmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "AssignmentPeriods",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "AssignmentPeriods",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentPeriods_ProjectId",
                table: "AssignmentPeriods",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentPeriods_Projects_ProjectId",
                table: "AssignmentPeriods",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Backfill: Mevcut dönemlerin ProjectId'sini Assignment üzerinden doldur
            migrationBuilder.Sql(@"
                UPDATE ""AssignmentPeriods"" ap
                SET ""ProjectId"" = a.""ProjectId""
                FROM ""Assignments"" a
                WHERE ap.""AssignmentId"" = a.""Id""
                  AND ap.""ProjectId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentPeriods_Projects_ProjectId",
                table: "AssignmentPeriods");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentPeriods_ProjectId",
                table: "AssignmentPeriods");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "AssignmentPeriods");

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "AssignmentPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
