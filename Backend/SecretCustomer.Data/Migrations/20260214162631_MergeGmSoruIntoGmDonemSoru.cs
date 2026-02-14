using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeGmSoruIntoGmDonemSoru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "GmSoruId",
                table: "GmDonemSorular",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AranmaSayisi",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BeklenenCevap",
                table: "GmDonemSorular",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GmHedefFirmaId",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsKuponlu",
                table: "GmDonemSorular",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SiraNo",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SoruMetni",
                table: "GmDonemSorular",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            // Mevcut verileri GmSorular tablosundan kopyala (PostgreSQL syntax)
            migrationBuilder.Sql(@"
                UPDATE ""GmDonemSorular"" ds
                SET ""SoruMetni"" = s.""SoruMetni"",
                    ""BeklenenCevap"" = s.""BeklenenCevap"",
                    ""AranmaSayisi"" = s.""AranmaSayisi"",
                    ""IsKuponlu"" = s.""IsKuponlu"",
                    ""SiraNo"" = s.""SiraNo"",
                    ""CustomerId"" = s.""CustomerId"",
                    ""GmHedefFirmaId"" = s.""GmHedefFirmaId""
                FROM ""GmSorular"" s
                WHERE ds.""GmSoruId"" = s.""Id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemSorular_CustomerId",
                table: "GmDonemSorular",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDonemSorular_GmHedefFirmaId",
                table: "GmDonemSorular",
                column: "GmHedefFirmaId");

            migrationBuilder.AddForeignKey(
                name: "FK_GmDonemSorular_Customers_CustomerId",
                table: "GmDonemSorular",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GmDonemSorular_GmHedefFirmalar_GmHedefFirmaId",
                table: "GmDonemSorular",
                column: "GmHedefFirmaId",
                principalTable: "GmHedefFirmalar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GmDonemSorular_Customers_CustomerId",
                table: "GmDonemSorular");

            migrationBuilder.DropForeignKey(
                name: "FK_GmDonemSorular_GmHedefFirmalar_GmHedefFirmaId",
                table: "GmDonemSorular");

            migrationBuilder.DropIndex(
                name: "IX_GmDonemSorular_CustomerId",
                table: "GmDonemSorular");

            migrationBuilder.DropIndex(
                name: "IX_GmDonemSorular_GmHedefFirmaId",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "AranmaSayisi",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "BeklenenCevap",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "GmHedefFirmaId",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "IsKuponlu",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "SiraNo",
                table: "GmDonemSorular");

            migrationBuilder.DropColumn(
                name: "SoruMetni",
                table: "GmDonemSorular");

            migrationBuilder.AlterColumn<int>(
                name: "GmSoruId",
                table: "GmDonemSorular",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
