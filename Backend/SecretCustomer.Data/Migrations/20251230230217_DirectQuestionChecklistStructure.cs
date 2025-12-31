using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class DirectQuestionChecklistStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Yeni ChecklistId kolonunu ekle
            migrationBuilder.AddColumn<int>(
                name: "ChecklistId",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // 2. ChecklistId'yi Section'dan doldur (mevcut sorular için)
            migrationBuilder.Sql(@"
                UPDATE ""Questions"" q
                SET ""ChecklistId"" = s.""ChecklistId""
                FROM ""Sections"" s
                WHERE q.""SectionId"" = s.""Id""
            ");

            // 3. ScaleSteps kolonunu ekle (varsayılan 4 kırılım)
            migrationBuilder.AddColumn<int>(
                name: "ScaleSteps",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            // 4. LikertScale kolonunu Checklists tablosuna ekle
            migrationBuilder.AddColumn<int>(
                name: "LikertScale",
                table: "Checklists",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            // 5. Eski kolonları kaldır (Type, Points, MaxPoints, OptionsJson)
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "Questions");

            // 6. SectionId'yi nullable yap
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Sections_SectionId",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "Questions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // 7. RecommendedNote ve HelpText için max length ekle
            migrationBuilder.AlterColumn<string>(
                name: "RecommendedNote",
                table: "Questions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HelpText",
                table: "Questions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // 8. ChecklistId için index ve foreign key ekle
            migrationBuilder.CreateIndex(
                name: "IX_Questions_ChecklistId",
                table: "Questions",
                column: "ChecklistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Checklists_ChecklistId",
                table: "Questions",
                column: "ChecklistId",
                principalTable: "Checklists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 9. SectionId için yeni foreign key (SetNull ile)
            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Sections_SectionId",
                table: "Questions",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Checklists_ChecklistId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Sections_SectionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ChecklistId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LikertScale",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "ChecklistId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ScaleSteps",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "SectionId",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RecommendedNote",
                table: "Questions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HelpText",
                table: "Questions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPoints",
                table: "Questions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "Questions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Sections_SectionId",
                table: "Questions",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
