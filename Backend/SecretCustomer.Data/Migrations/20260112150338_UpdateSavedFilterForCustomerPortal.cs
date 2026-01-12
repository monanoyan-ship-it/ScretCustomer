using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSavedFilterForCustomerPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_Users_UserId",
                table: "SavedFilters");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "SavedFilters",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "SavedFilters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedFilters_CustomerId",
                table: "SavedFilters",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_Customers_CustomerId",
                table: "SavedFilters",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_Users_UserId",
                table: "SavedFilters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_Customers_CustomerId",
                table: "SavedFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedFilters_Users_UserId",
                table: "SavedFilters");

            migrationBuilder.DropIndex(
                name: "IX_SavedFilters_CustomerId",
                table: "SavedFilters");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "SavedFilters");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "SavedFilters",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedFilters_Users_UserId",
                table: "SavedFilters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
