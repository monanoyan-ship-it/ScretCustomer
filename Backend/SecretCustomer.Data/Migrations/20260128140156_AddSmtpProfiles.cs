using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmtpProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    FromEmail = table.Column<string>(type: "text", nullable: false),
                    FromName = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    UseOAuth = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<string>(type: "text", nullable: true),
                    ClientSecret = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpProfiles", x => x.Id);
                });

            // Data migration: Mevcut SystemSettings Smtp.* kayıtlarını SmtpProfiles tablosuna aktar
            migrationBuilder.Sql(@"
                INSERT INTO ""SmtpProfiles"" (
                    ""Name"", ""Host"", ""Port"", ""Username"", ""Password"",
                    ""UseSsl"", ""FromEmail"", ""FromName"", ""Enabled"", ""IsDefault"",
                    ""UseOAuth"", ""TenantId"", ""ClientId"", ""ClientSecret"",
                    ""CreatedAt"", ""IsDeleted""
                )
                SELECT
                    'Varsayılan Profil',
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.Host' THEN ""Value"" END), ''),
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.Port' THEN ""Value"" END)::int, 587),
                    MAX(CASE WHEN ""Key"" = 'Smtp.Username' THEN ""Value"" END),
                    MAX(CASE WHEN ""Key"" = 'Smtp.Password' THEN ""Value"" END),
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.UseSsl' THEN ""Value"" END) = 'true', true),
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.FromEmail' THEN ""Value"" END), ''),
                    MAX(CASE WHEN ""Key"" = 'Smtp.FromName' THEN ""Value"" END),
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.Enabled' THEN ""Value"" END) = 'true', true),
                    true,
                    COALESCE(MAX(CASE WHEN ""Key"" = 'Smtp.UseOAuth' THEN ""Value"" END) = 'true', false),
                    MAX(CASE WHEN ""Key"" = 'Smtp.TenantId' THEN ""Value"" END),
                    MAX(CASE WHEN ""Key"" = 'Smtp.ClientId' THEN ""Value"" END),
                    MAX(CASE WHEN ""Key"" = 'Smtp.ClientSecret' THEN ""Value"" END),
                    NOW(),
                    false
                FROM ""SystemSettings""
                WHERE ""Key"" LIKE 'Smtp.%'
                HAVING COUNT(*) > 0;
            ");

            // Eski Smtp.* SystemSettings kayıtlarını sil
            migrationBuilder.Sql(@"
                DELETE FROM ""SystemSettings"" WHERE ""Key"" LIKE 'Smtp.%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmtpProfiles");
        }
    }
}
