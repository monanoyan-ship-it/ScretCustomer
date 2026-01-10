using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleSupervisorsPerPersonnelInOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnelOrganizations_CustomerPersonnelId_Customer~",
                table: "CustomerPersonnelOrganizations");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizations_CustomerPersonnelId_Customer~",
                table: "CustomerPersonnelOrganizations",
                columns: new[] { "CustomerPersonnelId", "CustomerOrganizationId", "SupervisorId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnelOrganizations_CustomerPersonnelId_Customer~",
                table: "CustomerPersonnelOrganizations");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizations_CustomerPersonnelId_Customer~",
                table: "CustomerPersonnelOrganizations",
                columns: new[] { "CustomerPersonnelId", "CustomerOrganizationId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
