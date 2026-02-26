using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGmDinlemeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GmDinlemeAyarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDonemId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ChecklistId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDinlemeAyarlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAyarlar_Checklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "Checklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAyarlar_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAyarlar_GmDonemler_GmDonemId",
                        column: x => x.GmDonemId,
                        principalTable: "GmDonemler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GmDinlemeEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmAtamaId = table.Column<int>(type: "integer", nullable: false),
                    ChecklistId = table.Column<int>(type: "integer", nullable: false),
                    DinleyenUserId = table.Column<int>(type: "integer", nullable: false),
                    DurumId = table.Column<int>(type: "integer", nullable: false),
                    DinlemeTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    YellowCardCount = table.Column<int>(type: "integer", nullable: false),
                    RedCardCount = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDinlemeEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDinlemeEvaluations_Checklists_ChecklistId",
                        column: x => x.ChecklistId,
                        principalTable: "Checklists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GmDinlemeEvaluations_GmAtamalar_GmAtamaId",
                        column: x => x.GmAtamaId,
                        principalTable: "GmAtamalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GmDinlemeEvaluations_Users_DinleyenUserId",
                        column: x => x.DinleyenUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GmDinlemeAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GmDinlemeEvaluationId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerNumeric = table.Column<int>(type: "integer", nullable: true),
                    AnswerText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    GivenPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EarnedPoints = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApplyPenalty = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDinlemeAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAnswers_GmDinlemeEvaluations_GmDinlemeEvaluationId",
                        column: x => x.GmDinlemeEvaluationId,
                        principalTable: "GmDinlemeEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GmDinlemeAnswerSubCriteria",
                columns: table => new
                {
                    GmDinlemeAnswerId = table.Column<int>(type: "integer", nullable: false),
                    SubCriteriaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmDinlemeAnswerSubCriteria", x => new { x.GmDinlemeAnswerId, x.SubCriteriaId });
                    table.ForeignKey(
                        name: "FK_GmDinlemeAnswerSubCriteria_GmDinlemeAnswers_GmDinlemeAnswer~",
                        column: x => x.GmDinlemeAnswerId,
                        principalTable: "GmDinlemeAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GmDinlemeAnswerSubCriteria_QuestionSubCriteria_SubCriteriaId",
                        column: x => x.SubCriteriaId,
                        principalTable: "QuestionSubCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAnswers_GmDinlemeEvaluationId",
                table: "GmDinlemeAnswers",
                column: "GmDinlemeEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAnswers_QuestionId",
                table: "GmDinlemeAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAnswerSubCriteria_SubCriteriaId",
                table: "GmDinlemeAnswerSubCriteria",
                column: "SubCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAyarlar_ChecklistId",
                table: "GmDinlemeAyarlar",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAyarlar_CustomerId",
                table: "GmDinlemeAyarlar",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeAyarlar_GmDonemId_CustomerId",
                table: "GmDinlemeAyarlar",
                columns: new[] { "GmDonemId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeEvaluations_ChecklistId",
                table: "GmDinlemeEvaluations",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeEvaluations_DinleyenUserId",
                table: "GmDinlemeEvaluations",
                column: "DinleyenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeEvaluations_DurumId",
                table: "GmDinlemeEvaluations",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_GmDinlemeEvaluations_GmAtamaId",
                table: "GmDinlemeEvaluations",
                column: "GmAtamaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GmDinlemeAnswerSubCriteria");

            migrationBuilder.DropTable(
                name: "GmDinlemeAyarlar");

            migrationBuilder.DropTable(
                name: "GmDinlemeAnswers");

            migrationBuilder.DropTable(
                name: "GmDinlemeEvaluations");
        }
    }
}
