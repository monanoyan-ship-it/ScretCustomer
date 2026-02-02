using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPersonnelIdToCustomerDealer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactPersonnelId",
                table: "CustomerDealers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealers_ContactPersonnelId",
                table: "CustomerDealers",
                column: "ContactPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDealers_CustomerPersonnel_ContactPersonnelId",
                table: "CustomerDealers",
                column: "ContactPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDealers_CustomerPersonnel_ContactPersonnelId",
                table: "CustomerDealers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDealers_ContactPersonnelId",
                table: "CustomerDealers");

            migrationBuilder.DropColumn(
                name: "ContactPersonnelId",
                table: "CustomerDealers");
        }
    }
}
