using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganizationId",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CustomerOrganizationId",
                table: "Assignments",
                column: "CustomerOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_CustomerOrganizations_CustomerOrganizationId",
                table: "Assignments",
                column: "CustomerOrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_CustomerOrganizations_CustomerOrganizationId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_CustomerOrganizationId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CustomerOrganizationId",
                table: "Assignments");
        }
    }
}
