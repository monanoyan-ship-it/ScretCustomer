using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Evaluations tablosu için performans index'leri
            // 610K+ kayıt için kritik
            // NOT: Id zaten Primary Key olarak indexli - ek index gerekmez

            // CreatedAt - tarih filtreleme için
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Evaluations_CreatedAt""
                ON ""Evaluations"" (""CreatedAt"" DESC);
            ");

            // CompletedAt - tarih filtreleme için
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Evaluations_CompletedAt""
                ON ""Evaluations"" (""CompletedAt"" DESC NULLS LAST);
            ");

            // CallDate - tarih filtreleme için
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Evaluations_CallDate""
                ON ""Evaluations"" (""CallDate"" DESC NULLS LAST);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Evaluations_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Evaluations_CompletedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Evaluations_CallDate"";");
        }
    }
}
