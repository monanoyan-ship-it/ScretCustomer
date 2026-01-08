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
    public DbSet<Section> Sections { get; set; }
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
    public DbSet<CustomerOrganizationManager> CustomerOrganizationManagers { get; set; }
    public DbSet<CustomerPersonnelOrganizationAccess> CustomerPersonnelOrganizationAccess { get; set; }

    // Permission Management DbSets
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    // Project Management DbSets
    public DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; }

    // Personnel Management DbSets
    public DbSet<Personnel> Personnel { get; set; }

    // Question Attachments
    public DbSet<QuestionAttachment> QuestionAttachments { get; set; }

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

    // Personnel Requests (Personel Talepleri)
    public DbSet<PersonnelRequest> PersonnelRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Checklist>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Section>().HasQueryFilter(e => !e.IsDeleted);
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
        modelBuilder.Entity<CustomerOrganizationManager>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelOrganizationAccess>().HasQueryFilter(e => !e.IsDeleted);

        // Permission Management Entities
        modelBuilder.Entity<Permission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(e => !e.IsDeleted);

        // Project Management Entities
        modelBuilder.Entity<ProjectTeamMember>().HasQueryFilter(e => !e.IsDeleted);

        // Personnel Management Entities
        modelBuilder.Entity<Personnel>().HasQueryFilter(e => !e.IsDeleted);

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

        // ===== CustomerOrganization İlişkileri =====

        // CustomerOrganization self-referencing (Parent-Children)
        modelBuilder.Entity<CustomerOrganization>()
            .HasOne(o => o.Parent)
            .WithMany(o => o.Children)
            .HasForeignKey(o => o.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerPersonnel self-referencing (Supervisor-TeamMembers)
        modelBuilder.Entity<CustomerPersonnel>()
            .HasOne(p => p.Supervisor)
            .WithMany(p => p.TeamMembers)
            .HasForeignKey(p => p.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerPersonnel-Organization relationship
        modelBuilder.Entity<CustomerPersonnel>()
            .HasOne(p => p.Organization)
            .WithMany(o => o.Personnel)
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // CustomerOrganizationManager (many-to-many: Personnel yönettiği organizasyonlar)
        modelBuilder.Entity<CustomerOrganizationManager>()
            .HasOne(m => m.CustomerPersonnel)
            .WithMany(p => p.ManagedOrganizations)
            .HasForeignKey(m => m.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerOrganizationManager>()
            .HasOne(m => m.CustomerOrganization)
            .WithMany(o => o.Managers)
            .HasForeignKey(m => m.CustomerOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomerPersonnelOrganizationAccess (many-to-many: Personnel değerlendirebileceği organizasyonlar)
        modelBuilder.Entity<CustomerPersonnelOrganizationAccess>()
            .HasOne(a => a.CustomerPersonnel)
            .WithMany(p => p.OrganizationAccess)
            .HasForeignKey(a => a.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerPersonnelOrganizationAccess>()
            .HasOne(a => a.CustomerOrganization)
            .WithMany(o => o.EvaluatorAccess)
            .HasForeignKey(a => a.CustomerOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // ===== Checklist -> Questions İlişkisi (Section kaldırıldı) =====
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Checklist)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Section ilişkisi (geriye uyumluluk - opsiyonel)
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Section)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.SectionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

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
            .HasIndex(a => a.CreatedAt);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.LogType);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Category);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.TableName, a.RecordId });

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


