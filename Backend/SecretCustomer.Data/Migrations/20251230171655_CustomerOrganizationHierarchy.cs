using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class CustomerOrganizationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvaluatedCustomerPersonnelId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluatedOrganizationId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "CustomerPersonnel",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizations_CustomerOrganizations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CustomerOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrganizationManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrganizationManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizationManagers_CustomerOrganizations_Customer~",
                        column: x => x.CustomerOrganizationId,
                        principalTable: "CustomerOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerOrganizationManagers_CustomerPersonnel_CustomerPers~",
                        column: x => x.CustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPersonnelOrganizationAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CanEvaluate = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPersonnelOrganizationAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelOrganizationAccess_CustomerOrganizations_C~",
                        column: x => x.CustomerOrganizationId,
                        principalTable: "CustomerOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPersonnelOrganizationAccess_CustomerPersonnel_Custo~",
                        column: x => x.CustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_EvaluatedCustomerPersonnelId",
                table: "Evaluations",
                column: "EvaluatedCustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_EvaluatedOrganizationId",
                table: "Evaluations",
                column: "EvaluatedOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_OrganizationId",
                table: "CustomerPersonnel",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationManagers_CustomerOrganizationId",
                table: "CustomerOrganizationManagers",
                column: "CustomerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizationManagers_CustomerPersonnelId",
                table: "CustomerOrganizationManagers",
                column: "CustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizations_CustomerId",
                table: "CustomerOrganizations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganizations_ParentId",
                table: "CustomerOrganizations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizationAccess_CustomerOrganizationId",
                table: "CustomerPersonnelOrganizationAccess",
                column: "CustomerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizationAccess_CustomerPersonnelId",
                table: "CustomerPersonnelOrganizationAccess",
                column: "CustomerPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizations_OrganizationId",
                table: "CustomerPersonnel",
                column: "OrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel",
                column: "SupervisorId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_CustomerOrganizations_EvaluatedOrganizationId",
                table: "Evaluations",
                column: "EvaluatedOrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_CustomerPersonnel_EvaluatedCustomerPersonnelId",
                table: "Evaluations",
                column: "EvaluatedCustomerPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerOrganizations_OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_CustomerOrganizations_EvaluatedOrganizationId",
                table: "Evaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_CustomerPersonnel_EvaluatedCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropTable(
                name: "CustomerOrganizationManagers");

            migrationBuilder.DropTable(
                name: "CustomerPersonnelOrganizationAccess");

            migrationBuilder.DropTable(
                name: "CustomerOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_EvaluatedCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_EvaluatedOrganizationId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_SupervisorId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "EvaluatedCustomerPersonnelId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "EvaluatedOrganizationId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "CustomerPersonnel");
        }
    }
}
