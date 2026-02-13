using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGolgeMusteriTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GmDonemler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    OlusturanUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDonemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDonemler_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmDonemler_Users_OlusturanUserId",
                        column: x => x.OlusturanUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GmHedefFirmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    FirmaAdi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TelefonNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmHedefFirmalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmHedefFirmalar_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GmDonemKuponlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDonemId = table.Column<int>(type: "integer", nullable: false),
                    KuponKodu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDonemKuponlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDonemKuponlar_GmDonemler_GmDonemId",
                        column: x => x.GmDonemId,
                        principalTable: "GmDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GmDonemPersoneller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDonemId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDonemPersoneller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDonemPersoneller_GmDonemler_GmDonemId",
                        column: x => x.GmDonemId,
                        principalTable: "GmDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmDonemPersoneller_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GmSorular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    GmHedefFirmaId = table.Column<int>(type: "integer", nullable: false),
                    SoruMetni = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    BeklenenCevap = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AranmaSayisi = table.Column<int>(type: "integer", nullable: false),
                    IsKuponlu = table.Column<bool>(type: "boolean", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmSorular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmSorular_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmSorular_GmHedefFirmalar_GmHedefFirmaId",
                        column: x => x.GmHedefFirmaId,
                        principalTable: "GmHedefFirmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GmDonemSorular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDonemId = table.Column<int>(type: "integer", nullable: false),
                    GmSoruId = table.Column<int>(type: "integer", nullable: false),
                    AranmaSayisi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDonemSorular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDonemSorular_GmDonemler_GmDonemId",
                        column: x => x.GmDonemId,
                        principalTable: "GmDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmDonemSorular_GmSorular_GmSoruId",
                        column: x => x.GmSoruId,
                        principalTable: "GmSorular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GmAtamalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDonemId = table.Column<int>(type: "integer", nullable: false),
                    GmDonemSoruId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlanTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GerceklesmeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AramaSaati = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Not = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KuponKodu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmAtamalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmAtamalar_GmDonemSorular_GmDonemSoruId",
                        column: x => x.GmDonemSoruId,
                        principalTable: "GmDonemSorular",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GmAtamalar_GmDonemler_GmDonemId",
                        column: x => x.GmDonemId,
                        principalTable: "GmDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmAtamalar_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GmAtamalar_DurumId",
                table: "GmAtamalar",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_GmAtamalar_GmDonemId",
                table: "GmAtamalar",
                column: "GmDonemId");

            migrationBuilder.CreateIndex(
                name: "IX_GmAtamalar_GmDonemSoruId",
                table: "GmAtamalar",
                column: "GmDonemSoruId");

            migrationBuilder.CreateIndex(
                name: "IX_GmAtamalar_UserId",
                table: "GmAtamalar",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemKuponlar_GmDonemId",
                table: "GmDonemKuponlar",
                column: "GmDonemId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemler_CustomerId",
                table: "GmDonemler",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemler_DurumId",
                table: "GmDonemler",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemler_OlusturanUserId",
                table: "GmDonemler",
                column: "OlusturanUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemPersoneller_GmDonemId",
                table: "GmDonemPersoneller",
                column: "GmDonemId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemPersoneller_UserId",
                table: "GmDonemPersoneller",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemSorular_GmDonemId",
                table: "GmDonemSorular",
                column: "GmDonemId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemSorular_GmSoruId",
                table: "GmDonemSorular",
                column: "GmSoruId");

            migrationBuilder.CreateIndex(
                name: "IX_GmHedefFirmalar_CustomerId",
                table: "GmHedefFirmalar",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GmSorular_CustomerId",
                table: "GmSorular",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GmSorular_GmHedefFirmaId",
                table: "GmSorular",
                column: "GmHedefFirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GmAtamalar");

            migrationBuilder.DropTable(
                name: "GmDonemKuponlar");

            migrationBuilder.DropTable(
                name: "GmDonemPersoneller");

            migrationBuilder.DropTable(
                name: "GmDonemSorular");

            migrationBuilder.DropTable(
                name: "GmDonemler");

            migrationBuilder.DropTable(
                name: "GmSorular");

            migrationBuilder.DropTable(
                name: "GmHedefFirmalar");
        }
    }
}
