using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateOrganizationDataV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut OrganizationId verilerini junction table'a tasi
            // Zaten varsa atla
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerPersonnelOrganizations""
                    (""CustomerPersonnelId"", ""CustomerOrganizationId"", ""SupervisorId"", ""AssignedAt"", ""Notes"", ""CreatedAt"", ""IsDeleted"")
                SELECT
                    cp.""Id"",
                    cp.""OrganizationId"",
                    cp.""SupervisorId"",
                    NOW(),
                    'Migration V2 ile taşındı',
                    NOW(),
                    false
                FROM ""CustomerPersonnel"" cp
                WHERE cp.""OrganizationId"" IS NOT NULL
                  AND cp.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM ""CustomerPersonnelOrganizations"" cpo
                      WHERE cpo.""CustomerPersonnelId"" = cp.""Id""
                        AND cpo.""CustomerOrganizationId"" = cp.""OrganizationId""
                  )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
