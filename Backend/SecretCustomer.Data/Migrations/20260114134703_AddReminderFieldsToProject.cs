using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderFieldsToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SendReminderEmails kolonu ekle (eğer yoksa)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Projects' AND column_name = 'SendReminderEmails') THEN
                        ALTER TABLE ""Projects"" ADD ""SendReminderEmails"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");

            // ReminderDaysBeforeEnd kolonu ekle (eğer yoksa)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Projects' AND column_name = 'ReminderDaysBeforeEnd') THEN
                        ALTER TABLE ""Projects"" ADD ""ReminderDaysBeforeEnd"" integer;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""SendReminderEmails"";
                ALTER TABLE ""Projects"" DROP COLUMN IF EXISTS ""ReminderDaysBeforeEnd"";
            ");
        }
    }
}
