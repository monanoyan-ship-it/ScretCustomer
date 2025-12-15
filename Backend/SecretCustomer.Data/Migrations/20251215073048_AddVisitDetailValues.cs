using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitDetailValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitDetailValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerVisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Field = table.Column<int>(type: "integer", nullable: false),
                    IntValue = table.Column<int>(type: "integer", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BoolValue = table.Column<bool>(type: "boolean", nullable: true),
                    StringValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitDetailValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitDetailValues_CustomerVisits_CustomerVisitId",
                        column: x => x.CustomerVisitId,
                        principalTable: "CustomerVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitDetailValues_CustomerVisitId",
                table: "VisitDetailValues",
                column: "CustomerVisitId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitDetailValues");
        }
    }
}
