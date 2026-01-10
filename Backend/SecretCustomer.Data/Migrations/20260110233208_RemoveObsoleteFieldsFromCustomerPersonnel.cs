using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteFieldsFromCustomerPersonnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizations_OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "CustomerPersonnel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_OrganizationId",
                table: "CustomerPersonnel",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel",
                column: "SupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizations_OrganizationId",
                table: "CustomerPersonnel",
                column: "OrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel",
                column: "SupervisorId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
