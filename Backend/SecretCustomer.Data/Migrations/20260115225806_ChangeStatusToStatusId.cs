using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStatusToStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SurveyInvitations: Önce StatusId ekle, veriyi dönüştür, sonra Status sil
            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "SurveyInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Default: Pending

            // Status string -> StatusId int dönüşümü
            migrationBuilder.Sql(@"
                UPDATE ""SurveyInvitations"" SET ""StatusId"" = CASE
                    WHEN ""Status"" = 'Sent' THEN 2
                    WHEN ""Status"" = 'Failed' THEN 3
                    ELSE 1 -- Pending
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_SurveyInvitations_Status",
                table: "SurveyInvitations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SurveyInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInvitations_StatusId",
                table: "SurveyInvitations",
                column: "StatusId");

            // SurveyExternalInvitations: Önce StatusId ekle, veriyi dönüştür, sonra Status sil
            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "SurveyExternalInvitations",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Default: Pending

            // Status string -> StatusId int dönüşümü
            migrationBuilder.Sql(@"
                UPDATE ""SurveyExternalInvitations"" SET ""StatusId"" = CASE
                    WHEN ""Status"" = 'Sent' THEN 2
                    WHEN ""Status"" = 'Failed' THEN 3
                    ELSE 1 -- Pending
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_SurveyExternalInvitations_Status",
                table: "SurveyExternalInvitations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SurveyExternalInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyExternalInvitations_StatusId",
                table: "SurveyExternalInvitations",
                column: "StatusId");

            // SupportRequests tablosu
            migrationBuilder.CreateTable(
                name: "SupportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    CustomerPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    AdminResponse = table.Column<string>(type: "text", nullable: true),
                    RespondedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsReadByUser = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRequests_CustomerPersonnel_CustomerPersonnelId",
                        column: x => x.CustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_RespondedByUserId",
                        column: x => x.RespondedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CreatedAt",
                table: "SupportRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CustomerPersonnelId",
                table: "SupportRequests",
                column: "CustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_RespondedByUserId",
                table: "SupportRequests",
                column: "RespondedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_StatusId",
                table: "SupportRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_UserId",
                table: "SupportRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportRequests");

            // SurveyInvitations: StatusId -> Status geri dönüşüm
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SurveyInvitations",
                type: "text",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql(@"
                UPDATE ""SurveyInvitations"" SET ""Status"" = CASE
                    WHEN ""StatusId"" = 2 THEN 'Sent'
                    WHEN ""StatusId"" = 3 THEN 'Failed'
                    ELSE 'Pending'
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_SurveyInvitations_StatusId",
                table: "SurveyInvitations");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "SurveyInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInvitations_Status",
                table: "SurveyInvitations",
                column: "Status");

            // SurveyExternalInvitations: StatusId -> Status geri dönüşüm
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SurveyExternalInvitations",
                type: "text",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql(@"
                UPDATE ""SurveyExternalInvitations"" SET ""Status"" = CASE
                    WHEN ""StatusId"" = 2 THEN 'Sent'
                    WHEN ""StatusId"" = 3 THEN 'Failed'
                    ELSE 'Pending'
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_SurveyExternalInvitations_StatusId",
                table: "SurveyExternalInvitations");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "SurveyExternalInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyExternalInvitations_Status",
                table: "SurveyExternalInvitations",
                column: "Status");
        }
    }
}
