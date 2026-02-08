using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistIdToEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Assignments_AssignmentId",
                table: "Evaluations");

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "Evaluations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // 1. ChecklistId kolonunu nullable olarak ekle (backfill için)
            migrationBuilder.AddColumn<int>(
                name: "ChecklistId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            // 2. Mevcut kayıtlara Assignment'tan ChecklistId'yi kopyala
            migrationBuilder.Sql(
                @"UPDATE ""Evaluations"" e SET ""ChecklistId"" = a.""ChecklistId"" FROM ""Assignments"" a WHERE e.""AssignmentId"" = a.""Id"";");

            // 3. ChecklistId'si hala null olanlar varsa (Assignment silinmiş olabilir), ilk checklist'i ata
            migrationBuilder.Sql(
                @"UPDATE ""Evaluations"" SET ""ChecklistId"" = (SELECT ""Id"" FROM ""Checklists"" ORDER BY ""Id"" LIMIT 1) WHERE ""ChecklistId"" IS NULL;");

            // 4. Artık NOT NULL yapabiliriz
            migrationBuilder.AlterColumn<int>(
                name: "ChecklistId",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_ChecklistId",
                table: "Evaluations",
                column: "ChecklistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Assignments_AssignmentId",
                table: "Evaluations",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Checklists_ChecklistId",
                table: "Evaluations",
                column: "ChecklistId",
                principalTable: "Checklists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Assignments_AssignmentId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Checklists_ChecklistId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_ChecklistId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "ChecklistId",
                table: "Evaluations");

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Assignments_AssignmentId",
                table: "Evaluations",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
