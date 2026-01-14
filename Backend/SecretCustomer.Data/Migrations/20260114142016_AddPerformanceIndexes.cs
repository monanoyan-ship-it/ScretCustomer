using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecretCustomer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Her index için ayrı try-catch benzeri DO block kullan
            // Böylece bir hata diğerlerini etkilemez

            // Evaluations indexes
            CreateIndexSafe(migrationBuilder, "Evaluations", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Evaluations", "AssignmentId");
            CreateIndexSafe(migrationBuilder, "Evaluations", "EvaluatedCustomerPersonnelId");
            CreateIndexSafe(migrationBuilder, "Evaluations", "EvaluatedOrganizationId");
            CreateIndexSafe(migrationBuilder, "Evaluations", "CompletedAt");
            CreateIndexSafe(migrationBuilder, "Evaluations", "CreatedAt");
            CreateIndexSafe(migrationBuilder, "Evaluations", "EvaluatorId");

            // Assignments indexes
            CreateIndexSafe(migrationBuilder, "Assignments", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Assignments", "ProjectId");
            CreateIndexSafe(migrationBuilder, "Assignments", "ChecklistId");
            CreateIndexSafe(migrationBuilder, "Assignments", "AssignedUserId");
            CreateIndexSafe(migrationBuilder, "Assignments", "AssignedCustomerPersonnelId");
            CreateIndexSafe(migrationBuilder, "Assignments", "IsCompleted");
            CreateIndexSafe(migrationBuilder, "Assignments", "DueDate");

            // Answers indexes
            CreateIndexSafe(migrationBuilder, "Answers", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Answers", "EvaluationId");
            CreateIndexSafe(migrationBuilder, "Answers", "QuestionId");

            // Projects indexes
            CreateIndexSafe(migrationBuilder, "Projects", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Projects", "CustomerId");
            CreateIndexSafe(migrationBuilder, "Projects", "OrganizationId");
            CreateIndexSafe(migrationBuilder, "Projects", "ChecklistId");
            CreateIndexSafe(migrationBuilder, "Projects", "StartDate");
            CreateIndexSafe(migrationBuilder, "Projects", "EndDate");

            // CustomerPersonnel indexes
            CreateIndexSafe(migrationBuilder, "CustomerPersonnel", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "CustomerPersonnel", "CustomerId");
            CreateIndexSafe(migrationBuilder, "CustomerPersonnel", "IsActive");

            // CustomerOrganizations indexes
            CreateIndexSafe(migrationBuilder, "CustomerOrganizations", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "CustomerOrganizations", "CustomerId");
            CreateIndexSafe(migrationBuilder, "CustomerOrganizations", "ParentId");

            // CustomerPersonnelOrganizations indexes
            CreateIndexSafe(migrationBuilder, "CustomerPersonnelOrganizations", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "CustomerPersonnelOrganizations", "CustomerPersonnelId");
            CreateIndexSafe(migrationBuilder, "CustomerPersonnelOrganizations", "CustomerOrganizationId");

            // Questions indexes
            CreateIndexSafe(migrationBuilder, "Questions", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Questions", "ChecklistId");

            // Checklists indexes
            CreateIndexSafe(migrationBuilder, "Checklists", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Checklists", "CustomerId");

            // Customers indexes
            CreateIndexSafe(migrationBuilder, "Customers", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Customers", "IsActive");

            // Users indexes
            CreateIndexSafe(migrationBuilder, "Users", "IsDeleted");
            CreateIndexSafe(migrationBuilder, "Users", "IsActive");
        }

        private void CreateIndexSafe(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            var indexName = $"IX_{tableName}_{columnName}";
            // PostgreSQL stores identifiers in lowercase in information_schema
            migrationBuilder.Sql($@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = lower('{indexName}')) THEN
                        CREATE INDEX ""{indexName}"" ON ""{tableName}"" (""{columnName}"");
                    END IF;
                EXCEPTION WHEN OTHERS THEN
                    RAISE NOTICE 'Index {indexName} skipped: %', SQLERRM;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Index'leri sil
            var tables = new[] { "Evaluations", "Assignments", "Answers", "Projects",
                "CustomerPersonnel", "CustomerOrganizations", "CustomerPersonnelOrganizations",
                "Questions", "Checklists", "Customers", "Users" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($@"
                    DO $$
                    DECLARE
                        idx RECORD;
                    BEGIN
                        FOR idx IN SELECT indexname FROM pg_indexes WHERE tablename = '{table}' AND indexname LIKE 'IX_{table}_%'
                        LOOP
                            EXECUTE 'DROP INDEX IF EXISTS ""' || idx.indexname || '""';
                        END LOOP;
                    END $$;
                ");
            }
        }
    }
}
