using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistIdAndAssignmentIdToEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. AssignmentId nullable olarak ekle
            migrationBuilder.AddColumn<int>(
                name: "AssignmentId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            // 2. ChecklistId nullable olarak ekle (backfill sonrası NOT NULL yapacağız)
            migrationBuilder.AddColumn<int>(
                name: "ChecklistId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            // 3. Mevcut verileri backfill et - Project üzerinden ChecklistId al
            migrationBuilder.Sql(@"
                UPDATE ""Evaluations"" e
                SET ""ChecklistId"" = p.""ChecklistId""
                FROM ""Projects"" p
                WHERE e.""ProjectId"" = p.""Id""
            ");

            // 3b. Mevcut verileri backfill et - AssignmentId'yi eşleşen atamadan al
            migrationBuilder.Sql(@"
                UPDATE ""Evaluations"" e
                SET ""AssignmentId"" = a.""Id""
                FROM ""Assignments"" a
                WHERE a.""ProjectId"" = e.""ProjectId""
                  AND a.""IsDeleted"" = false
                  AND (
                    (e.""EvaluatorId"" IS NOT NULL AND a.""AssignedUserId"" = e.""EvaluatorId"")
                    OR
                    (e.""EvaluatorCustomerPersonnelId"" IS NOT NULL AND a.""AssignedCustomerPersonnelId"" = e.""EvaluatorCustomerPersonnelId"")
                  )
            ");

            // 4. ChecklistId'yi NOT NULL yap
            migrationBuilder.AlterColumn<int>(
                name: "ChecklistId",
                table: "Evaluations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 5. Index'ler
            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_AssignmentId",
                table: "Evaluations",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_ChecklistId",
                table: "Evaluations",
                column: "ChecklistId");

            // 6. FK'ler
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
                name: "IX_Evaluations_AssignmentId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_ChecklistId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "ChecklistId",
                table: "Evaluations");
        }
    }
}
