using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupByAndAggregateToExcelTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupByPropertyName",
                table: "ExcelTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AggregateTypeId",
                table: "ExcelColumns",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupByPropertyName",
                table: "ExcelTemplates");

            migrationBuilder.DropColumn(
                name: "AggregateTypeId",
                table: "ExcelColumns");
        }
    }
}
