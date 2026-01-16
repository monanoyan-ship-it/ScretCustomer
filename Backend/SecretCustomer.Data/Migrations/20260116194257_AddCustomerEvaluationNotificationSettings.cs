using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEvaluationNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailTemplates_Customers_CustomerId",
                table: "EmailTemplates");

            migrationBuilder.AddColumn<int>(
                name: "EvaluationNotificationFrequencyId",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EvaluationNotificationTemplateId",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotificationSentAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationEmails",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_EvaluationNotificationTemplateId",
                table: "Customers",
                column: "EvaluationNotificationTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_EmailTemplates_EvaluationNotificationTemplateId",
                table: "Customers",
                column: "EvaluationNotificationTemplateId",
                principalTable: "EmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailTemplates_Customers_CustomerId",
                table: "EmailTemplates",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_EmailTemplates_EvaluationNotificationTemplateId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailTemplates_Customers_CustomerId",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Customers_EvaluationNotificationTemplateId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EvaluationNotificationFrequencyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EvaluationNotificationTemplateId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastNotificationSentAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NotificationEmails",
                table: "Customers");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailTemplates_Customers_CustomerId",
                table: "EmailTemplates",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }
    }
}
