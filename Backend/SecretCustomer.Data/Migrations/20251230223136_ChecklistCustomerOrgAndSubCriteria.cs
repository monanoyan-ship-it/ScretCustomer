using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChecklistCustomerOrgAndSubCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Checklist tablosuna CustomerId ve CustomerOrganizationId ekle
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganizationId",
                table: "Checklists",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CustomerId",
                table: "Checklists",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CustomerOrganizationId",
                table: "Checklists",
                column: "CustomerOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_Customers_CustomerId",
                table: "Checklists",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Checklists_CustomerOrganizations_CustomerOrganizationId",
                table: "Checklists",
                column: "CustomerOrganizationId",
                principalTable: "CustomerOrganizations",
                principalColumn: "Id");

            // QuestionSubCriteria tablosu
            migrationBuilder.CreateTable(
                name: "QuestionSubCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    WeightPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionSubCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionSubCriteria_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnswerSubCriteriaSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnswerId = table.Column<int>(type: "integer", nullable: false),
                    SubCriteriaId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerSubCriteriaSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerSubCriteriaSelections_Answers_AnswerId",
                        column: x => x.AnswerId,
                        principalTable: "Answers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswerSubCriteriaSelections_QuestionSubCriteria_SubCriteria~",
                        column: x => x.SubCriteriaId,
                        principalTable: "QuestionSubCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSubCriteriaSelections_AnswerId",
                table: "AnswerSubCriteriaSelections",
                column: "AnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswerSubCriteriaSelections_SubCriteriaId",
                table: "AnswerSubCriteriaSelections",
                column: "SubCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSubCriteria_QuestionId",
                table: "QuestionSubCriteria",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerSubCriteriaSelections");

            migrationBuilder.DropTable(
                name: "QuestionSubCriteria");

            // Checklist tablosundan CustomerId ve CustomerOrganizationId kaldır
            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_Customers_CustomerId",
                table: "Checklists");

            migrationBuilder.DropForeignKey(
                name: "FK_Checklists_CustomerOrganizations_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_CustomerId",
                table: "Checklists");

            migrationBuilder.DropIndex(
                name: "IX_Checklists_CustomerOrganizationId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "CustomerOrganizationId",
                table: "Checklists");
        }
    }
}
