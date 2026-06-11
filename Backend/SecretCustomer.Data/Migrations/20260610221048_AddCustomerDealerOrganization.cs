using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDealerOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDealerOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerDealerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDealerOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDealerOrganizations_CustomerDealers_CustomerDealerId",
                        column: x => x.CustomerDealerId,
                        principalTable: "CustomerDealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerDealerOrganizations_CustomerOrganizations_CustomerO~",
                        column: x => x.CustomerOrganizationId,
                        principalTable: "CustomerOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerOrganizations_CustomerDealerId",
                table: "CustomerDealerOrganizations",
                column: "CustomerDealerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerOrganizations_CustomerOrganizationId",
                table: "CustomerDealerOrganizations",
                column: "CustomerOrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDealerOrganizations");
        }
    }
}
