using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTrainingVideoParticipantToCustomerPersonnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingVideoParticipants_Users_UserId",
                table: "TrainingVideoParticipants");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TrainingVideoParticipants",
                newName: "CustomerPersonnelId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingVideoParticipants_UserId",
                table: "TrainingVideoParticipants",
                newName: "IX_TrainingVideoParticipants_CustomerPersonnelId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingVideoParticipants_TrainingVideoAssignmentId_UserId",
                table: "TrainingVideoParticipants",
                newName: "IX_TrainingVideoParticipants_TrainingVideoAssignmentId_Custome~");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingVideoParticipants_CustomerPersonnel_CustomerPersonn~",
                table: "TrainingVideoParticipants",
                column: "CustomerPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingVideoParticipants_CustomerPersonnel_CustomerPersonn~",
                table: "TrainingVideoParticipants");

            migrationBuilder.RenameColumn(
                name: "CustomerPersonnelId",
                table: "TrainingVideoParticipants",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingVideoParticipants_TrainingVideoAssignmentId_Custome~",
                table: "TrainingVideoParticipants",
                newName: "IX_TrainingVideoParticipants_TrainingVideoAssignmentId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainingVideoParticipants_CustomerPersonnelId",
                table: "TrainingVideoParticipants",
                newName: "IX_TrainingVideoParticipants_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingVideoParticipants_Users_UserId",
                table: "TrainingVideoParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
