using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLanguagePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreferredLanguageId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredLanguageId",
                table: "CustomerPersonnel",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PreferredLanguageId",
                table: "Users",
                column: "PreferredLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPersonnel_PreferredLanguageId",
                table: "CustomerPersonnel",
                column: "PreferredLanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPersonnel_Languages_PreferredLanguageId",
                table: "CustomerPersonnel",
                column: "PreferredLanguageId",
                principalTable: "Languages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Languages_PreferredLanguageId",
                table: "Users",
                column: "PreferredLanguageId",
                principalTable: "Languages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPersonnel_Languages_PreferredLanguageId",
                table: "CustomerPersonnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Languages_PreferredLanguageId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PreferredLanguageId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPersonnel_PreferredLanguageId",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "PreferredLanguageId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredLanguageId",
                table: "CustomerPersonnel");
        }
    }
}
