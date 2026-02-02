using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class DealerToCustomerDealerRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_Dealers_DealerId",
                table: "Evaluations");

            migrationBuilder.DropTable(
                name: "DealerRequests");

            migrationBuilder.DropTable(
                name: "Dealers");

            migrationBuilder.RenameColumn(
                name: "DealerId",
                table: "Evaluations",
                newName: "CustomerDealerId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_DealerId",
                table: "Evaluations",
                newName: "IX_Evaluations_CustomerDealerId");

            migrationBuilder.CreateTable(
                name: "CustomerDealers",
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
                    table.PrimaryKey("PK_CustomerDealers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDealers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentCustomerDealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    CustomerDealerId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentCustomerDealers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentCustomerDealers_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentCustomerDealers_CustomerDealers_CustomerDealerId",
                        column: x => x.CustomerDealerId,
                        principalTable: "CustomerDealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentCustomerDealers_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDealerRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    RequestTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CustomerDealerId = table.Column<int>(type: "integer", nullable: true),
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
                name: "IX_AssignmentCustomerDealers_AssignmentId",
                table: "AssignmentCustomerDealers",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCustomerDealers_CustomerDealerId",
                table: "AssignmentCustomerDealers",
                column: "CustomerDealerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCustomerDealers_EvaluationId",
                table: "AssignmentCustomerDealers",
                column: "EvaluationId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealers_Code",
                table: "CustomerDealers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDealers_CustomerId",
                table: "CustomerDealers",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_CustomerDealers_CustomerDealerId",
                table: "Evaluations",
                column: "CustomerDealerId",
                principalTable: "CustomerDealers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evaluations_CustomerDealers_CustomerDealerId",
                table: "Evaluations");

            migrationBuilder.DropTable(
                name: "AssignmentCustomerDealers");

            migrationBuilder.DropTable(
                name: "CustomerDealerRequests");

            migrationBuilder.DropTable(
                name: "CustomerDealers");

            migrationBuilder.RenameColumn(
                name: "CustomerDealerId",
                table: "Evaluations",
                newName: "DealerId");

            migrationBuilder.RenameIndex(
                name: "IX_Evaluations_CustomerDealerId",
                table: "Evaluations",
                newName: "IX_Evaluations_DealerId");

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DealerTypeId = table.Column<int>(type: "integer", nullable: false),
                    District = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    WorkingHoursJson = table.Column<string>(type: "text", nullable: true)
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
                    DealerId = table.Column<int>(type: "integer", nullable: true),
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
    }
}
