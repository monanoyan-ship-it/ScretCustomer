using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerScoreThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDealerRequests");

            migrationBuilder.CreateTable(
                name: "CustomerScoreThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "integer", nullable: false),
                    SuccessThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    WarningThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerScoreThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerScoreThresholds_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerScoreThresholds_CustomerId_ProjectTypeId",
                table: "CustomerScoreThresholds",
                columns: new[] { "CustomerId", "ProjectTypeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerScoreThresholds");

            migrationBuilder.CreateTable(
                name: "CustomerDealerRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerDealerId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProcessedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AdminResponse = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestDataJson = table.Column<string>(type: "text", nullable: false),
                    RequestTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDealerRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDealerRequests_CustomerDealers_CustomerDealerId",
                        column: x => x.CustomerDealerId,
                        principalTable: "CustomerDealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerDealerRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerDealerRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerDealerRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerRequests_CustomerDealerId",
                table: "CustomerDealerRequests",
                column: "CustomerDealerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerRequests_CustomerId",
                table: "CustomerDealerRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerRequests_ProcessedByUserId",
                table: "CustomerDealerRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerRequests_RequestedByUserId",
                table: "CustomerDealerRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealerRequests_StatusId",
                table: "CustomerDealerRequests",
                column: "StatusId");
        }
    }
}
