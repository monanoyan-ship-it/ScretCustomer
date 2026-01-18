using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoPlaybackControlFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowSkipping",
                table: "TrainingVideos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPlaybackSpeed",
                table: "TrainingVideos",
                type: "numeric",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<int>(
                name: "MinWatchPercentage",
                table: "TrainingVideos",
                type: "integer",
                nullable: false,
                defaultValue: 90);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowSkipping",
                table: "TrainingVideos");

            migrationBuilder.DropColumn(
                name: "MaxPlaybackSpeed",
                table: "TrainingVideos");

            migrationBuilder.DropColumn(
                name: "MinWatchPercentage",
                table: "TrainingVideos");
        }
    }
}
