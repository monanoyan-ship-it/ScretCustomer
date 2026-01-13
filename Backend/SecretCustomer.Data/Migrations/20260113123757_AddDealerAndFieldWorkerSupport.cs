using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealerAndFieldWorkerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DealerId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitId",
                table: "Evaluations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    WorkingHoursJson = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dealers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DealerRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RequestTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    DealerId = table.Column<int>(type: "integer", nullable: true),
                    RequestDataJson = table.Column<string>(type: "text", nullable: false),
                    AdminResponse = table.Column<string>(type: "text", nullable: true),
                    ProcessedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealerRequests_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DealerRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealerRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_DealerId",
                table: "Evaluations",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_VisitId",
                table: "Evaluations",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerRequests_CustomerId",
                table: "DealerRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerRequests_DealerId",
                table: "DealerRequests",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerRequests_ProcessedByUserId",
                table: "DealerRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerRequests_RequestedByUserId",
                table: "DealerRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerRequests_StatusId",
                table: "DealerRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_Code",
                table: "Dealers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_CustomerId",
                table: "Dealers",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Dealers_DealerId",
                table: "Evaluations",
                column: "DealerId",
                principalTable: "Dealers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Dealers_DealerId",
                table: "Evaluations");

            migrationBuilder.DropTable(
                name: "DealerRequests");

            migrationBuilder.DropTable(
                name: "Dealers");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_DealerId",
                table: "Evaluations");

            migrationBuilder.DropIndex(
                name: "IX_Evaluations_VisitId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "DealerId",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "Evaluations");
        }
    }
}
