using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using System.Reflection;

namespace SecretCustomer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }
    public DbSet<AssignmentPeriod> AssignmentPeriods { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<ExcelTemplate> ExcelTemplates { get; set; }
    public DbSet<ExcelColumn> ExcelColumns { get; set; }
    
    // Customer Management DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerPersonnel> CustomerPersonnel { get; set; }
    public DbSet<CustomerTaskList> CustomerTaskLists { get; set; }
    public DbSet<CustomerPersonnelTaskAssignment> CustomerPersonnelTaskAssignments { get; set; }
    public DbSet<CustomerPersonnelPermission> CustomerPersonnelPermissions { get; set; }

    // Customer Organization DbSets (Yeni Hiyerarşi)
    public DbSet<CustomerOrganization> CustomerOrganizations { get; set; }
    public DbSet<CustomerPersonnelOrganization> CustomerPersonnelOrganizations { get; set; }

    // Permission Management DbSets
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    // Project Management DbSets
    public DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; }
    public DbSet<ProjectFile> ProjectFiles { get; set; }

    // Email Templates
    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    // Question Attachments
    public DbSet<QuestionAttachment> QuestionAttachments { get; set; }

    // Evaluation Attachments
    public DbSet<EvaluationAttachment> EvaluationAttachments { get; set; }

    // Question Sub Criteria (Alt Kriterler/Öneriler)
    public DbSet<QuestionSubCriteria> QuestionSubCriteria { get; set; }
    public DbSet<AnswerSubCriteriaSelection> AnswerSubCriteriaSelections { get; set; }

    // Announcements
    public DbSet<Announcement> Announcements { get; set; }

    // Meetings
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
    public DbSet<MeetingAttachment> MeetingAttachments { get; set; }

    // Trainings
    public DbSet<Training> Trainings { get; set; }
    public DbSet<TrainingParticipant> TrainingParticipants { get; set; }
    public DbSet<TrainingMaterial> TrainingMaterials { get; set; }

    // Approvals and Notifications
    public DbSet<Approval> Approvals { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationSetting> NotificationSettings { get; set; }

    // App Settings (tek satırlık ayar tablosu)
    public DbSet<AppSettings> AppSettings { get; set; }

    // Localization - Çoklu Dil Desteği
    public DbSet<Language> Languages { get; set; }
    public DbSet<LocaleStringResource> LocaleStringResources { get; set; }

    // Audit Logs (Sistem logları)
    public DbSet<AuditLog> AuditLogs { get; set; }

    // System Settings (Sistem ayarları)
    public DbSet<SystemSetting> SystemSettings { get; set; }

    // Saved Filters (Kaydedilmiş filtreler)
    public DbSet<SavedFilter> SavedFilters { get; set; }

    // Personnel Requests (Personel Talepleri)
    public DbSet<PersonnelRequest> PersonnelRequests { get; set; }

    // FieldWorker - Dealer Management (Bayi Yönetimi)
    public DbSet<Dealer> Dealers { get; set; }
    public DbSet<DealerRequest> DealerRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Checklist>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Question>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Assignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Evaluation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Answer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExcelTemplate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExcelColumn>().HasQueryFilter(e => !e.IsDeleted);
        
        // Customer Management Entities
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnel>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerTaskList>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelTaskAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelPermission>().HasQueryFilter(e => !e.IsDeleted);

        // Customer Organization Entities (Yeni Hiyerarşi)
        modelBuilder.Entity<CustomerOrganization>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelOrganization>().HasQueryFilter(e => !e.IsDeleted);

        // Permission Management Entities
        modelBuilder.Entity<Permission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(e => !e.IsDeleted);

        // Project Management Entities
        modelBuilder.Entity<ProjectTeamMember>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProjectFile>().HasQueryFilter(e => !e.IsDeleted);

        // Question Attachments
        modelBuilder.Entity<QuestionAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Question Sub Criteria
        modelBuilder.Entity<QuestionSubCriteria>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AnswerSubCriteriaSelection>().HasQueryFilter(e => !e.IsDeleted);

        // Announcements
        modelBuilder.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted);

        // Meetings
        modelBuilder.Entity<Meeting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MeetingParticipant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MeetingAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Trainings
        modelBuilder.Entity<Training>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingParticipant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingMaterial>().HasQueryFilter(e => !e.IsDeleted);

        // Approvals and Notifications
        modelBuilder.Entity<Approval>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NotificationSetting>().HasQueryFilter(e => !e.IsDeleted);

        // Personnel Requests
        modelBuilder.Entity<PersonnelRequest>().HasQueryFilter(e => !e.IsDeleted);

        // FieldWorker - Dealer Management
        modelBuilder.Entity<Dealer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DealerRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EvaluationAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // ===== CustomerOrganization İlişkileri =====

        // CustomerOrganization self-referencing (Parent-Children)
        modelBuilder.Entity<CustomerOrganization>()
            .HasOne(o => o.Parent)
            .WithMany(o => o.Children)
            .HasForeignKey(o => o.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Evaluation - EvaluatedCustomerPersonnel relationship
        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.EvaluatedCustomerPersonnel)
            .WithMany()
            .HasForeignKey(e => e.EvaluatedCustomerPersonnelId)
            .OnDelete(DeleteBehavior.SetNull);

        // Evaluation - EvaluatedOrganization relationship
        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.EvaluatedOrganization)
            .WithMany()
            .HasForeignKey(e => e.EvaluatedOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // ===== Checklist -> Questions İlişkisi =====
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Checklist)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== QuestionSubCriteria İlişkileri =====
        modelBuilder.Entity<QuestionSubCriteria>()
            .HasOne(sc => sc.Question)
            .WithMany(q => q.SubCriteria)
            .HasForeignKey(sc => sc.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== AnswerSubCriteriaSelection İlişkileri =====
        modelBuilder.Entity<AnswerSubCriteriaSelection>()
            .HasOne(s => s.Answer)
            .WithMany(a => a.SubCriteriaSelections)
            .HasForeignKey(s => s.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AnswerSubCriteriaSelection>()
            .HasOne(s => s.SubCriteria)
            .WithMany(sc => sc.Selections)
            .HasForeignKey(s => s.SubCriteriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // AuditLog indexleri (hızlı sorgu için)
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.LogTypeId).HasColumnName("LogType");
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.CreatedAt);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.LogTypeId);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Category);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.TableName, a.RecordId });

        // AssignmentPeriod Status mapping
        modelBuilder.Entity<AssignmentPeriod>()
            .Property(p => p.StatusId).HasColumnName("Status");

        // AppSettings ValueType mapping
        modelBuilder.Entity<AppSettings>()
            .Property(s => s.ValueTypeId).HasColumnName("ValueType");

        // ===== PersonnelRequest İlişkileri =====
        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.Evaluation)
            .WithMany()
            .HasForeignKey(pr => pr.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.Customer)
            .WithMany()
            .HasForeignKey(pr => pr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.CustomerOrganization)
            .WithMany()
            .HasForeignKey(pr => pr.CustomerOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.RequestedByUser)
            .WithMany()
            .HasForeignKey(pr => pr.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(pr => pr.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PersonnelRequest>()
            .HasOne(pr => pr.CreatedPersonnel)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedPersonnelId)
            .OnDelete(DeleteBehavior.SetNull);

        // PersonnelRequest indexleri
        modelBuilder.Entity<PersonnelRequest>()
            .HasIndex(pr => pr.Status);
        modelBuilder.Entity<PersonnelRequest>()
            .HasIndex(pr => pr.EvaluationId);

        // ===== Dealer İlişkileri =====
        modelBuilder.Entity<Dealer>()
            .HasOne(d => d.Customer)
            .WithMany(c => c.Dealers)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Dealer indexleri
        modelBuilder.Entity<Dealer>()
            .HasIndex(d => d.CustomerId);
        modelBuilder.Entity<Dealer>()
            .HasIndex(d => d.Code);

        // ===== DealerRequest İlişkileri =====
        modelBuilder.Entity<DealerRequest>()
            .HasOne(dr => dr.Customer)
            .WithMany()
            .HasForeignKey(dr => dr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DealerRequest>()
            .HasOne(dr => dr.RequestedByUser)
            .WithMany()
            .HasForeignKey(dr => dr.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DealerRequest>()
            .HasOne(dr => dr.Dealer)
            .WithMany()
            .HasForeignKey(dr => dr.DealerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DealerRequest>()
            .HasOne(dr => dr.ProcessedByUser)
            .WithMany()
            .HasForeignKey(dr => dr.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // DealerRequest indexleri
        modelBuilder.Entity<DealerRequest>()
            .HasIndex(dr => dr.StatusId);
        modelBuilder.Entity<DealerRequest>()
            .HasIndex(dr => dr.CustomerId);
        modelBuilder.Entity<DealerRequest>()
            .HasIndex(dr => dr.RequestedByUserId);

        // ===== Evaluation - Dealer İlişkisi =====
        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.Dealer)
            .WithMany(d => d.Evaluations)
            .HasForeignKey(e => e.DealerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Evaluation VisitId indexi
        modelBuilder.Entity<Evaluation>()
            .HasIndex(e => e.VisitId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}


