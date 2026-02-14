using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerIdFromGmDonem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GmDonemler_Customers_CustomerId",
                table: "GmDonemler");

            migrationBuilder.DropIndex(
                name: "IX_GmDonemler_CustomerId",
                table: "GmDonemler");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "GmDonemler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "GmDonemler",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemler_CustomerId",
                table: "GmDonemler",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GmDonemler_Customers_CustomerId",
                table: "GmDonemler",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
