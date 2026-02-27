using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKuponBilgisiToGmAtama_RemoveKuponKoduFromGmDonemSoru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KuponKodu",
                table: "GmDonemSorular");

            migrationBuilder.AddColumn<string>(
                name: "KuponBilgisi",
                table: "GmAtamalar",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KuponBilgisi",
                table: "GmAtamalar");

            migrationBuilder.AddColumn<string>(
                name: "KuponKodu",
                table: "GmDonemSorular",
                type: "text",
                nullable: true);
        }
    }
}
