using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestoreChecklistOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganizationId",
                table: "Checklists",
                type: "integer",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_CustomerOrganizations_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "CustomerOrganizationId",
                table: "Checklists");

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
    }
}
