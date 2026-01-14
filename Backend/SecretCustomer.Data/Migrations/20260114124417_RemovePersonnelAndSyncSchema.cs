using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersonnelAndSyncSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Personnel tablosu ve ilişkili veriler zaten silindi (önceki başarısız migration'da)
            // Bu migration sadece snapshot'ı güncellemek için - database zaten güncel durumda

            // NOT: DealerId, VisitId, Dealers, DealerRequests, EmailTemplates ve
            // Projects survey alanları database'de zaten mevcut.
            // Bu migration sadece model snapshot'ını senkronize etmek için.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down metodu: Personnel tablosunu ve PersonnelId kolonunu geri ekler
            migrationBuilder.AddColumn<int>(
                name: "PersonnelId",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_PersonnelId",
                table: "Evaluations",
                column: "PersonnelId");

            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ErpNo = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    SicilNo = table.Column<string>(type: "text", nullable: true),
                    TcKimlikNo = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personnel_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_CustomerId",
                table: "Personnel",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evaluations_Personnel_PersonnelId",
                table: "Evaluations",
                column: "PersonnelId",
                principalTable: "Personnel",
                principalColumn: "Id");
        }
    }
}
