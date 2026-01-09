using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerPersonnelOrganizationAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPersonnelOrganizationAccess");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerPersonnelOrganizationAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CustomerPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CanEvaluate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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
                name: "IX_CustomerPersonnelOrganizationAccess_CustomerOrganizationId",
                table: "CustomerPersonnelOrganizationAccess",
                column: "CustomerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnelOrganizationAccess_CustomerPersonnelId",
                table: "CustomerPersonnelOrganizationAccess",
                column: "CustomerPersonnelId");
        }
    }
}
