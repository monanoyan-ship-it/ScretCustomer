using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedByCustomerPersonnelToApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Users_ApprovedByUserId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Users_ApproverUserId",
                table: "Approvals");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedByUserId",
                table: "Approvals",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByCustomerPersonnelId",
                table: "Approvals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_RequestedByCustomerPersonnelId",
                table: "Approvals",
                column: "RequestedByCustomerPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_CustomerPersonnel_RequestedByCustomerPersonnelId",
                table: "Approvals",
                column: "RequestedByCustomerPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Users_ApprovedByUserId",
                table: "Approvals",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Users_ApproverUserId",
                table: "Approvals",
                column: "ApproverUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_CustomerPersonnel_RequestedByCustomerPersonnelId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Users_ApprovedByUserId",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Users_ApproverUserId",
                table: "Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_RequestedByCustomerPersonnelId",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "RequestedByCustomerPersonnelId",
                table: "Approvals");

            migrationBuilder.AlterColumn<int>(
                name: "RequestedByUserId",
                table: "Approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Users_ApprovedByUserId",
                table: "Approvals",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Users_ApproverUserId",
                table: "Approvals",
                column: "ApproverUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
