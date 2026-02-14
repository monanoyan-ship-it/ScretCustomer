using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAranmaSayisiFromGmDonemSoru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AranmaSayisi",
                table: "GmDonemSorular");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AranmaSayisi",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
