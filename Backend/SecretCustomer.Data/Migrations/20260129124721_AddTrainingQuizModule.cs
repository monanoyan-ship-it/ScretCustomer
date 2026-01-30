using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingQuizModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingQuizzes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingVideoId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PassingScore = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "boolean", nullable: false),
                    ShuffleOptions = table.Column<bool>(type: "boolean", nullable: false),
                    ShowResults = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizzes_TrainingVideos_TrainingVideoId",
                        column: x => x.TrainingVideoId,
                        principalTable: "TrainingVideos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingQuizQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingQuizId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    HelpText = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    QuestionTypeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizQuestions_TrainingQuizzes_TrainingQuizId",
                        column: x => x.TrainingQuizId,
                        principalTable: "TrainingQuizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingQuizResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingQuizId = table.Column<int>(type: "integer", nullable: false),
                    TrainingVideoParticipantId = table.Column<int>(type: "integer", nullable: true),
                    TrainingVideoExternalParticipantId = table.Column<int>(type: "integer", nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxPossibleScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ScorePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizResponses_TrainingQuizzes_TrainingQuizId",
                        column: x => x.TrainingQuizId,
                        principalTable: "TrainingQuizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingQuizResponses_TrainingVideoExternalParticipants_Tra~",
                        column: x => x.TrainingVideoExternalParticipantId,
                        principalTable: "TrainingVideoExternalParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingQuizResponses_TrainingVideoParticipants_TrainingVid~",
                        column: x => x.TrainingVideoParticipantId,
                        principalTable: "TrainingVideoParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrainingQuizOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingQuizQuestionId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    WeightPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizOptions_TrainingQuizQuestions_TrainingQuizQuest~",
                        column: x => x.TrainingQuizQuestionId,
                        principalTable: "TrainingQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingQuizAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingQuizResponseId = table.Column<int>(type: "integer", nullable: false),
                    TrainingQuizQuestionId = table.Column<int>(type: "integer", nullable: false),
                    SelectedOptionId = table.Column<int>(type: "integer", nullable: true),
                    SelectedOptionIds = table.Column<string>(type: "text", nullable: true),
                    EarnedPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingQuizAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuizOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "TrainingQuizOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuizQuestions_TrainingQuizQuest~",
                        column: x => x.TrainingQuizQuestionId,
                        principalTable: "TrainingQuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingQuizAnswers_TrainingQuizResponses_TrainingQuizRespo~",
                        column: x => x.TrainingQuizResponseId,
                        principalTable: "TrainingQuizResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_SelectedOptionId",
                table: "TrainingQuizAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TrainingQuizQuestionId",
                table: "TrainingQuizAnswers",
                column: "TrainingQuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizAnswers_TrainingQuizResponseId",
                table: "TrainingQuizAnswers",
                column: "TrainingQuizResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizOptions_TrainingQuizQuestionId",
                table: "TrainingQuizOptions",
                column: "TrainingQuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizQuestions_TrainingQuizId",
                table: "TrainingQuizQuestions",
                column: "TrainingQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizResponses_TrainingQuizId",
                table: "TrainingQuizResponses",
                column: "TrainingQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizResponses_TrainingVideoExternalParticipantId",
                table: "TrainingQuizResponses",
                column: "TrainingVideoExternalParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizResponses_TrainingVideoParticipantId",
                table: "TrainingQuizResponses",
                column: "TrainingVideoParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingQuizzes_TrainingVideoId",
                table: "TrainingQuizzes",
                column: "TrainingVideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingQuizAnswers");

            migrationBuilder.DropTable(
                name: "TrainingQuizOptions");

            migrationBuilder.DropTable(
                name: "TrainingQuizResponses");

            migrationBuilder.DropTable(
                name: "TrainingQuizQuestions");

            migrationBuilder.DropTable(
                name: "TrainingQuizzes");
        }
    }
}
