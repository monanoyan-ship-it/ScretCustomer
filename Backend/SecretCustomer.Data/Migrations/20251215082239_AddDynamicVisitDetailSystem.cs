using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicVisitDetailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_CustomerVisitId_Field",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_Field",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_Field_BoolValue",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_Field_IntValue",
                table: "VisitDetailValues");

            migrationBuilder.DropColumn(
                name: "Field",
                table: "VisitDetailValues");

            migrationBuilder.AddColumn<Guid>(
                name: "FieldDefinitionId",
                table: "VisitDetailValues",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "VisitSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitSectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRating = table.Column<int>(type: "integer", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HelpText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitFieldDefinitions_VisitSectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "VisitSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_CustomerVisitId_FieldDefinitionId",
                table: "VisitDetailValues",
                columns: new[] { "CustomerVisitId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId",
                table: "VisitDetailValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId_BoolValue",
                table: "VisitDetailValues",
                columns: new[] { "FieldDefinitionId", "BoolValue" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId_IntValue",
                table: "VisitDetailValues",
                columns: new[] { "FieldDefinitionId", "IntValue" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitFieldDefinitions_SectorId_Category_SortOrder",
                table: "VisitFieldDefinitions",
                columns: new[] { "SectorId", "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitFieldDefinitions_SectorId_Code",
                table: "VisitFieldDefinitions",
                columns: new[] { "SectorId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSectors_Code",
                table: "VisitSectors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSectors_SortOrder",
                table: "VisitSectors",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitDetailValues_VisitFieldDefinitions_FieldDefinitionId",
                table: "VisitDetailValues",
                column: "FieldDefinitionId",
                principalTable: "VisitFieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VisitDetailValues_VisitFieldDefinitions_FieldDefinitionId",
                table: "VisitDetailValues");

            migrationBuilder.DropTable(
                name: "VisitFieldDefinitions");

            migrationBuilder.DropTable(
                name: "VisitSectors");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_CustomerVisitId_FieldDefinitionId",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId_BoolValue",
                table: "VisitDetailValues");

            migrationBuilder.DropIndex(
                name: "IX_VisitDetailValues_FieldDefinitionId_IntValue",
                table: "VisitDetailValues");

            migrationBuilder.DropColumn(
                name: "FieldDefinitionId",
                table: "VisitDetailValues");

            migrationBuilder.AddColumn<int>(
                name: "Field",
                table: "VisitDetailValues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_CustomerVisitId_Field",
                table: "VisitDetailValues",
                columns: new[] { "CustomerVisitId", "Field" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_Field",
                table: "VisitDetailValues",
                column: "Field");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_Field_BoolValue",
                table: "VisitDetailValues",
                columns: new[] { "Field", "BoolValue" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_Field_IntValue",
                table: "VisitDetailValues",
                columns: new[] { "Field", "IntValue" });
        }
    }
}
