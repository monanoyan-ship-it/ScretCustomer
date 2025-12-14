using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LanguageCulture = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UniqueSeoCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FlagImageFileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rtl = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocaleStringResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResourceValue = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleStringResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocaleStringResources_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Languages_IsActive",
                table: "Languages",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_UniqueSeoCode",
                table: "Languages",
                column: "UniqueSeoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocaleStringResources_LanguageId_ResourceName",
                table: "LocaleStringResources",
                columns: new[] { "LanguageId", "ResourceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocaleStringResources_ResourceName",
                table: "LocaleStringResources",
                column: "ResourceName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocaleStringResources");

            migrationBuilder.DropTable(
                name: "Languages");
        }
    }
}
