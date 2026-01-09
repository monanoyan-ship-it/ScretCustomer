using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangePersonnelUniqueIndexToCompanyBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_CustomerId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_Email",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_Username",
                table: "CustomerPersonnel");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_CustomerId_Email",
                table: "CustomerPersonnel",
                columns: new[] { "CustomerId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_CustomerId_Username",
                table: "CustomerPersonnel",
                columns: new[] { "CustomerId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_CustomerId_Email",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_CustomerId_Username",
                table: "CustomerPersonnel");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_CustomerId",
                table: "CustomerPersonnel",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_Email",
                table: "CustomerPersonnel",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_Username",
                table: "CustomerPersonnel",
                column: "Username",
                unique: true);
        }
    }
}
