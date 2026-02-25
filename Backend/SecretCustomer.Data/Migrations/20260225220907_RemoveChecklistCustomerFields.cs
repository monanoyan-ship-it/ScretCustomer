using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChecklistCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_CustomerOrganizations_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Customers_CustomerId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_CustomerId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "CustomerOrganizationId",
                table: "Checklists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganizationId",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CustomerId",
                table: "Checklists",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CustomerOrganizationId",
                table: "Checklists",
                column: "CustomerOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_CustomerOrganizations_CustomerOrganizationId",
                table: "Checklists",
                column: "CustomerOrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Customers_CustomerId",
                table: "Checklists",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }
    }
}
