using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionSelectionTypeAndShowScoreInput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectionTypeId",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 2); // Multiple (Çoklu Seçim)

            migrationBuilder.AddColumn<bool>(
                name: "ShowScoreInput",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: true); // Normal dinlemelerde puan girişi gösterilsin
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectionTypeId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ShowScoreInput",
                table: "Questions");
        }
    }
}
