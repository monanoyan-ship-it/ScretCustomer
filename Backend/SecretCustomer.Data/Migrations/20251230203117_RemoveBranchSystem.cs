using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBranchSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Branches_BranchId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Calls_Branches_BranchId",
                table: "Calls");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerVisits_Branches_BranchId",
                table: "CustomerVisits");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationUnits_Branches_BranchId",
                table: "OrganizationUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Branches_BranchId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ProjectBranches");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Users_BranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_BranchId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationUnits_BranchId",
                table: "OrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_CustomerVisits_BranchId",
                table: "CustomerVisits");

            migrationBuilder.DropIndex(
                name: "IX_Calls_BranchId",
                table: "Calls");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_BranchId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CustomerVisits");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Calls");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Assignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Personnel",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "OrganizationUnits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CustomerVisits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Calls",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TargetCount = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBranches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectBranches_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_BranchId",
                table: "Personnel",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_BranchId",
                table: "OrganizationUnits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVisits_BranchId",
                table: "CustomerVisits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Calls_BranchId",
                table: "Calls",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_BranchId",
                table: "Assignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CustomerId",
                table: "Branches",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBranches_BranchId",
                table: "ProjectBranches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBranches_ProjectId",
                table: "ProjectBranches",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Branches_BranchId",
                table: "Assignments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Calls_Branches_BranchId",
                table: "Calls",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerVisits_Branches_BranchId",
                table: "CustomerVisits",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationUnits_Branches_BranchId",
                table: "OrganizationUnits",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Branches_BranchId",
                table: "Personnel",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
