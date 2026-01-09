using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalEvaluationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvaluatorCustomerPersonnelId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Assignments",
                type: "integer",
                nullable: false,
                defaultValue: 2); // InternalUser = 2

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_EvaluatorCustomerPersonnelId",
                table: "Evaluations",
                column: "EvaluatorCustomerPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_CustomerPersonnel_EvaluatorCustomerPersonnelId",
                table: "Evaluations",
                column: "EvaluatorCustomerPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_CustomerPersonnel_EvaluatorCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_EvaluatorCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "EvaluatorCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Assignments");
        }
    }
}
