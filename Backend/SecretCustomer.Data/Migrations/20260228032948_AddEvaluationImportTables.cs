using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationImportSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ImportedRows = table.Column<int>(type: "integer", nullable: false),
                    PendingRows = table.Column<int>(type: "integer", nullable: false),
                    SkippedRows = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationImportSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationImportSessions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvaluationImportSessions_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationImportPendingRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportSessionId = table.Column<int>(type: "integer", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    RawDataJson = table.Column<string>(type: "text", nullable: false),
                    ParsedProjectName = table.Column<string>(type: "text", nullable: true),
                    ParsedEvaluatorName = table.Column<string>(type: "text", nullable: true),
                    ParsedEvaluatedPersonName = table.Column<string>(type: "text", nullable: true),
                    ParsedCallId = table.Column<string>(type: "text", nullable: true),
                    ParsedCallDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ParsedCallTime = table.Column<string>(type: "text", nullable: true),
                    ParsedDuration = table.Column<string>(type: "text", nullable: true),
                    ParsedComment = table.Column<string>(type: "text", nullable: true),
                    ParsedScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ParsedPeriod = table.Column<string>(type: "text", nullable: true),
                    ParsedPeriodMonth = table.Column<string>(type: "text", nullable: true),
                    ParsedCreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ParsedModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MatchedProjectId = table.Column<int>(type: "integer", nullable: true),
                    MatchedEvaluatorId = table.Column<int>(type: "integer", nullable: true),
                    MatchedCustomerPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    UnmatchedProjectValue = table.Column<string>(type: "text", nullable: true),
                    UnmatchedEvaluatorValue = table.Column<string>(type: "text", nullable: true),
                    UnmatchedPersonValue = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationImportPendingRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationImportPendingRows_CustomerPersonnel_MatchedCustom~",
                        column: x => x.MatchedCustomerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvaluationImportPendingRows_EvaluationImportSessions_Import~",
                        column: x => x.ImportSessionId,
                        principalTable: "EvaluationImportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationImportPendingRows_Evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "Evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvaluationImportPendingRows_Projects_MatchedProjectId",
                        column: x => x.MatchedProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvaluationImportPendingRows_Users_MatchedEvaluatorId",
                        column: x => x.MatchedEvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationImportUnmatchedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportSessionId = table.Column<int>(type: "integer", nullable: false),
                    ItemTypeId = table.Column<int>(type: "integer", nullable: false),
                    OriginalValue = table.Column<string>(type: "text", nullable: false),
                    AffectedRowCount = table.Column<int>(type: "integer", nullable: false),
                    ResolvedEntityId = table.Column<int>(type: "integer", nullable: true),
                    ResolutionActionId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationImportUnmatchedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationImportUnmatchedItems_EvaluationImportSessions_Imp~",
                        column: x => x.ImportSessionId,
                        principalTable: "EvaluationImportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationImportUnmatchedItems_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportPendingRows_EvaluationId",
                table: "EvaluationImportPendingRows",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportPendingRows_ImportSessionId",
                table: "EvaluationImportPendingRows",
                column: "ImportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportPendingRows_MatchedCustomerPersonnelId",
                table: "EvaluationImportPendingRows",
                column: "MatchedCustomerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportPendingRows_MatchedEvaluatorId",
                table: "EvaluationImportPendingRows",
                column: "MatchedEvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportPendingRows_MatchedProjectId",
                table: "EvaluationImportPendingRows",
                column: "MatchedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportSessions_CustomerId",
                table: "EvaluationImportSessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportSessions_UploadedByUserId",
                table: "EvaluationImportSessions",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportUnmatchedItems_ImportSessionId",
                table: "EvaluationImportUnmatchedItems",
                column: "ImportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationImportUnmatchedItems_ResolvedByUserId",
                table: "EvaluationImportUnmatchedItems",
                column: "ResolvedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationImportPendingRows");

            migrationBuilder.DropTable(
                name: "EvaluationImportUnmatchedItems");

            migrationBuilder.DropTable(
                name: "EvaluationImportSessions");
        }
    }
}
