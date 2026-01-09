using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateOrganizationIdToJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut OrganizationId verilerini junction table'a tasi
            // Sadece OrganizationId olan ve henuz junction'da olmayan kayitlar icin
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomerPersonnelOrganizations"" 
                    (""CustomerPersonnelId"", ""CustomerOrganizationId"", ""SupervisorId"", ""AssignedAt"", ""Notes"", ""CreatedAt"", ""IsDeleted"")
                SELECT 
                    cp.""Id"",
                    cp.""OrganizationId"",
                    cp.""SupervisorId"",
                    NOW(),
                    'Eski sistemden migrate edildi',
                    NOW(),
                    false
                FROM ""CustomerPersonnel"" cp
                WHERE cp.""OrganizationId"" IS NOT NULL
                  AND cp.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM ""CustomerPersonnelOrganizations"" cpo 
                      WHERE cpo.""CustomerPersonnelId"" = cp.""Id"" 
                        AND cpo.""CustomerOrganizationId"" = cp.""OrganizationId""
                        AND cpo.""IsDeleted"" = false
                  )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration - junction'dan geri almak mantikli degil
            // Sadece migrate edilen kayitlari sil
            migrationBuilder.Sql(@"
                DELETE FROM ""CustomerPersonnelOrganizations"" 
                WHERE ""Notes"" = 'Eski sistemden migrate edildi'
            ");
        }
    }
}
