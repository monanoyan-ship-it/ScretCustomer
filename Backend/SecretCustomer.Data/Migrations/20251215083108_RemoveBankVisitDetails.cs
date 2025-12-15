using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBankVisitDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankVisitDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankVisitDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerVisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    AirConditioningRating = table.Column<int>(type: "integer", nullable: true),
                    AtmAreaRating = table.Column<int>(type: "integer", nullable: true),
                    AtmCleanlinessRating = table.Column<int>(type: "integer", nullable: true),
                    AtmCount = table.Column<int>(type: "integer", nullable: true),
                    AtmUsabilityRating = table.Column<int>(type: "integer", nullable: true),
                    BrochuresAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    BusyCountersCount = table.Column<int>(type: "integer", nullable: true),
                    CleanlinessRating = table.Column<int>(type: "integer", nullable: true),
                    CounterAreaRating = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CrossSellOffered = table.Column<bool>(type: "boolean", nullable: false),
                    DisabledAccessAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    EntranceAreaRating = table.Column<int>(type: "integer", nullable: true),
                    EntryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FarewellReceived = table.Column<bool>(type: "boolean", nullable: false),
                    GreetingReceived = table.Column<bool>(type: "boolean", nullable: false),
                    ImprovementAreas = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LightingRating = table.Column<int>(type: "integer", nullable: true),
                    ManagerAreaRating = table.Column<int>(type: "integer", nullable: true),
                    OverallSatisfactionRating = table.Column<int>(type: "integer", nullable: true),
                    ProductOffered = table.Column<string>(type: "text", nullable: true),
                    QueueNumber = table.Column<string>(type: "text", nullable: true),
                    QueueSystemAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    QueueTicketTaken = table.Column<bool>(type: "boolean", nullable: false),
                    QueueWaitMinutes = table.Column<int>(type: "integer", nullable: true),
                    RecommendationScore = table.Column<int>(type: "integer", nullable: true),
                    Scenario = table.Column<int>(type: "integer", nullable: false),
                    ScenarioCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ScenarioDescription = table.Column<string>(type: "text", nullable: true),
                    SecurityPersonnelPresent = table.Column<bool>(type: "boolean", nullable: false),
                    ServiceDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    SignageRating = table.Column<int>(type: "integer", nullable: true),
                    StaffAppearanceRating = table.Column<int>(type: "integer", nullable: true),
                    StaffAttentivenessRating = table.Column<int>(type: "integer", nullable: true),
                    StaffCommunicationRating = table.Column<int>(type: "integer", nullable: true),
                    StaffCountObserved = table.Column<int>(type: "integer", nullable: true),
                    StaffHasNameTag = table.Column<bool>(type: "boolean", nullable: false),
                    StaffKnowledgeRating = table.Column<int>(type: "integer", nullable: true),
                    StaffName = table.Column<string>(type: "text", nullable: true),
                    Strengths = table.Column<string>(type: "text", nullable: true),
                    TotalCountersCount = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    WaitingAreaRating = table.Column<int>(type: "integer", nullable: true),
                    WorkingAtmCount = table.Column<int>(type: "integer", nullable: true),
                    WouldVisitAgain = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankVisitDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankVisitDetails_CustomerVisits_CustomerVisitId",
                        column: x => x.CustomerVisitId,
                        principalTable: "CustomerVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankVisitDetails_CustomerVisitId",
                table: "BankVisitDetails",
                column: "CustomerVisitId",
                unique: true);
        }
    }
}
