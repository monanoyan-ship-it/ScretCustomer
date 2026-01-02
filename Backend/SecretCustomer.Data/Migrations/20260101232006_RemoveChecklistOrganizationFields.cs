using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChecklistOrganizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "LikertScale",
                table: "Checklists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganizationId",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LikertScale",
                table: "Checklists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
    }
}
