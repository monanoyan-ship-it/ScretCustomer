using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class EvaluationDurationString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Yeni Duration (text) kolonunu ekle
            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Evaluations",
                type: "text",
                nullable: true);

            // 2. Mevcut DurationMinutes değerlerini Duration'a kopyala (varsa)
            migrationBuilder.Sql(
                "UPDATE \"Evaluations\" SET \"Duration\" = \"DurationMinutes\"::text WHERE \"DurationMinutes\" IS NOT NULL");

            // 3. Eski DurationMinutes kolonunu kaldır
            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Evaluations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eski DurationMinutes kolonunu geri ekle
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Evaluations",
                type: "integer",
                nullable: true);

            // Duration değerlerini geri dönüştür (mümkünse)
            migrationBuilder.Sql(
                "UPDATE \"Evaluations\" SET \"DurationMinutes\" = \"Duration\"::integer WHERE \"Duration\" IS NOT NULL AND \"Duration\" ~ '^[0-9]+$'");

            // Duration kolonunu kaldır
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Evaluations");
        }
    }
}
