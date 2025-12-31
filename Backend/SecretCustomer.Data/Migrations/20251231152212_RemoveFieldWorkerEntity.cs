using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFieldWorkerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_FieldWorkers_AssignedFieldWorkerId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_FieldWorkers_FieldWorkerId",
                table: "Assignments");

            migrationBuilder.DropTable(
                name: "FieldWorkers");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssignedFieldWorkerId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_FieldWorkerId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AssignedFieldWorkerId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "FieldWorkerId",
                table: "Assignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedFieldWorkerId",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FieldWorkerId",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FieldWorkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldWorkers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignedFieldWorkerId",
                table: "Assignments",
                column: "AssignedFieldWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_FieldWorkerId",
                table: "Assignments",
                column: "FieldWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldWorkers_UserId",
                table: "FieldWorkers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_FieldWorkers_AssignedFieldWorkerId",
                table: "Assignments",
                column: "AssignedFieldWorkerId",
                principalTable: "FieldWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_FieldWorkers_FieldWorkerId",
                table: "Assignments",
                column: "FieldWorkerId",
                principalTable: "FieldWorkers",
                principalColumn: "Id");
        }
    }
}
