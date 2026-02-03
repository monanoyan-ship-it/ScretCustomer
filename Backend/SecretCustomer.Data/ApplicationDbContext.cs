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
    public DbSet<CustomerPersonnelNotificationLog> CustomerPersonnelNotificationLogs { get; set; }

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

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationSetting> NotificationSettings { get; set; }

    // Approvals (Onay Talepleri)
    public DbSet<Approval> Approvals { get; set; }

    // Training Videos
    public DbSet<TrainingVideo> TrainingVideos { get; set; }
    public DbSet<TrainingVideoScope> TrainingVideoScopes { get; set; }
    public DbSet<TrainingVideoAssignment> TrainingVideoAssignments { get; set; }
    public DbSet<TrainingVideoParticipant> TrainingVideoParticipants { get; set; }
    public DbSet<TrainingVideoEmailLog> TrainingVideoEmailLogs { get; set; }
    public DbSet<TrainingVideoExternalParticipant> TrainingVideoExternalParticipants { get; set; }
    public DbSet<TrainingVideoExternalEmailLog> TrainingVideoExternalEmailLogs { get; set; }

    // Training Quiz (Eğitim Videosu Anketleri)
    public DbSet<TrainingQuiz> TrainingQuizzes { get; set; }
    public DbSet<TrainingQuizQuestion> TrainingQuizQuestions { get; set; }
    public DbSet<TrainingQuizOption> TrainingQuizOptions { get; set; }
    public DbSet<TrainingQuizResponse> TrainingQuizResponses { get; set; }
    public DbSet<TrainingQuizAnswer> TrainingQuizAnswers { get; set; }

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
    public DbSet<CustomerDealer> CustomerDealers { get; set; }
    public DbSet<AssignmentCustomerDealer> AssignmentCustomerDealers { get; set; }

    // Survey Invitations (Anket Davetiyeleri)
    public DbSet<SurveyInvitation> SurveyInvitations { get; set; }
    public DbSet<SurveyExternalInvitation> SurveyExternalInvitations { get; set; }

    // Performance Settings (Performans Ayarları)
    public DbSet<PerformanceSettings> PerformanceSettings { get; set; }

    // Customer Score Thresholds (Müşteri Bazlı Puan Eşikleri)
    public DbSet<CustomerScoreThreshold> CustomerScoreThresholds { get; set; }

    // Support Requests (Destek Talepleri)
    public DbSet<SupportRequest> SupportRequests { get; set; }

    // SMTP Profiles (SMTP Profilleri)
    public DbSet<SmtpProfile> SmtpProfiles { get; set; }

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

        // Notifications
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NotificationSetting>().HasQueryFilter(e => !e.IsDeleted);

        // Training Videos
        modelBuilder.Entity<TrainingVideo>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingVideoScope>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingVideoAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingVideoParticipant>().HasQueryFilter(e => !e.IsDeleted);

        // Training Quiz
        modelBuilder.Entity<TrainingQuiz>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingQuizQuestion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingQuizOption>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingQuizResponse>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingQuizAnswer>().HasQueryFilter(e => !e.IsDeleted);

        // Personnel Requests
        modelBuilder.Entity<PersonnelRequest>().HasQueryFilter(e => !e.IsDeleted);

        // FieldWorker - Dealer Management
        modelBuilder.Entity<CustomerDealer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AssignmentCustomerDealer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EvaluationAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Performance Settings
        modelBuilder.Entity<PerformanceSettings>().HasQueryFilter(e => !e.IsDeleted);

        // Customer Score Thresholds
        modelBuilder.Entity<CustomerScoreThreshold>().HasQueryFilter(e => !e.IsDeleted);

        // Support Requests
        modelBuilder.Entity<SupportRequest>().HasQueryFilter(e => !e.IsDeleted);

        // SMTP Profiles
        modelBuilder.Entity<SmtpProfile>().HasQueryFilter(e => !e.IsDeleted);

        // ===== Customer - EmailTemplate İlişkileri =====

        // EmailTemplate'in CustomerId'si -> Şablonun hangi müşteriye özel olduğu
        modelBuilder.Entity<EmailTemplate>()
            .HasOne(et => et.Customer)
            .WithMany()
            .HasForeignKey(et => et.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Customer'ın EvaluationNotificationTemplateId'si -> Müşterinin bildirim şablonu
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.EvaluationNotificationTemplate)
            .WithMany()
            .HasForeignKey(c => c.EvaluationNotificationTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // ===== CustomerDealer İlişkileri =====
        modelBuilder.Entity<CustomerDealer>()
            .HasOne(d => d.Customer)
            .WithMany(c => c.CustomerDealers)
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomerDealer indexleri
        modelBuilder.Entity<CustomerDealer>()
            .HasIndex(d => d.CustomerId);
        modelBuilder.Entity<CustomerDealer>()
            .HasIndex(d => d.Code);

        // ===== AssignmentCustomerDealer İlişkileri =====
        modelBuilder.Entity<AssignmentCustomerDealer>()
            .HasOne(acd => acd.Assignment)
            .WithMany(a => a.AssignmentCustomerDealers)
            .HasForeignKey(acd => acd.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentCustomerDealer>()
            .HasOne(acd => acd.CustomerDealer)
            .WithMany(cd => cd.AssignmentCustomerDealers)
            .HasForeignKey(acd => acd.CustomerDealerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentCustomerDealer>()
            .HasOne(acd => acd.Evaluation)
            .WithMany()
            .HasForeignKey(acd => acd.EvaluationId)
            .OnDelete(DeleteBehavior.SetNull);

        // AssignmentCustomerDealer indexleri
        modelBuilder.Entity<AssignmentCustomerDealer>()
            .HasIndex(acd => acd.AssignmentId);
        modelBuilder.Entity<AssignmentCustomerDealer>()
            .HasIndex(acd => acd.CustomerDealerId);

        // ===== Evaluation - CustomerDealer İlişkisi =====
        modelBuilder.Entity<Evaluation>()
            .HasOne(e => e.CustomerDealer)
            .WithMany(d => d.Evaluations)
            .HasForeignKey(e => e.CustomerDealerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Evaluation VisitId indexi
        modelBuilder.Entity<Evaluation>()
            .HasIndex(e => e.VisitId);

        // ===== SurveyInvitation İlişkileri =====
        modelBuilder.Entity<SurveyInvitation>()
            .HasOne(si => si.Project)
            .WithMany(p => p.SurveyInvitations)
            .HasForeignKey(si => si.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SurveyInvitation>()
            .HasOne(si => si.CustomerPersonnel)
            .WithMany()
            .HasForeignKey(si => si.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        // SurveyInvitation indexleri
        modelBuilder.Entity<SurveyInvitation>()
            .HasIndex(si => si.ProjectId);
        modelBuilder.Entity<SurveyInvitation>()
            .HasIndex(si => si.CustomerPersonnelId);
        modelBuilder.Entity<SurveyInvitation>()
            .HasIndex(si => si.StatusId);

        // ===== SurveyExternalInvitation İlişkileri =====
        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasOne(sei => sei.Project)
            .WithMany(p => p.SurveyExternalInvitations)
            .HasForeignKey(sei => sei.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasOne(sei => sei.Evaluation)
            .WithMany()
            .HasForeignKey(sei => sei.EvaluationId)
            .OnDelete(DeleteBehavior.SetNull);

        // SurveyExternalInvitation indexleri
        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasIndex(sei => sei.ProjectId);
        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasIndex(sei => sei.Token)
            .IsUnique();
        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasIndex(sei => sei.StatusId);
        modelBuilder.Entity<SurveyExternalInvitation>()
            .HasIndex(sei => sei.Email);

        // ===== SupportRequest İlişkileri =====
        modelBuilder.Entity<SupportRequest>()
            .HasOne(sr => sr.User)
            .WithMany()
            .HasForeignKey(sr => sr.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(sr => sr.CustomerPersonnel)
            .WithMany()
            .HasForeignKey(sr => sr.CustomerPersonnelId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SupportRequest>()
            .HasOne(sr => sr.RespondedByUser)
            .WithMany()
            .HasForeignKey(sr => sr.RespondedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // SupportRequest indexleri
        modelBuilder.Entity<SupportRequest>()
            .HasIndex(sr => sr.StatusId);
        modelBuilder.Entity<SupportRequest>()
            .HasIndex(sr => sr.UserId);
        modelBuilder.Entity<SupportRequest>()
            .HasIndex(sr => sr.CustomerPersonnelId);
        modelBuilder.Entity<SupportRequest>()
            .HasIndex(sr => sr.CreatedAt);

        // ===== CustomerScoreThreshold İlişkileri =====
        modelBuilder.Entity<CustomerScoreThreshold>()
            .HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: CustomerId + ProjectTypeId
        modelBuilder.Entity<CustomerScoreThreshold>()
            .HasIndex(s => new { s.CustomerId, s.ProjectTypeId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // ===== TrainingQuiz İlişkileri =====

        // TrainingQuiz -> TrainingVideo
        modelBuilder.Entity<TrainingQuiz>()
            .HasOne(q => q.TrainingVideo)
            .WithMany()
            .HasForeignKey(q => q.TrainingVideoId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingQuizQuestion -> TrainingQuiz
        modelBuilder.Entity<TrainingQuizQuestion>()
            .HasOne(q => q.TrainingQuiz)
            .WithMany(quiz => quiz.Questions)
            .HasForeignKey(q => q.TrainingQuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingQuizOption -> TrainingQuizQuestion
        modelBuilder.Entity<TrainingQuizOption>()
            .HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.TrainingQuizQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingQuizResponse -> TrainingQuiz
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasOne(r => r.TrainingQuiz)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.TrainingQuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingQuizResponse -> TrainingVideoParticipant
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasOne(r => r.Participant)
            .WithMany()
            .HasForeignKey(r => r.TrainingVideoParticipantId)
            .OnDelete(DeleteBehavior.SetNull);

        // TrainingQuizResponse -> TrainingVideoExternalParticipant
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasOne(r => r.ExternalParticipant)
            .WithMany()
            .HasForeignKey(r => r.TrainingVideoExternalParticipantId)
            .OnDelete(DeleteBehavior.SetNull);

        // TrainingQuizAnswer -> TrainingQuizResponse
        modelBuilder.Entity<TrainingQuizAnswer>()
            .HasOne(a => a.Response)
            .WithMany(r => r.Answers)
            .HasForeignKey(a => a.TrainingQuizResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingQuizAnswer -> TrainingQuizQuestion
        modelBuilder.Entity<TrainingQuizAnswer>()
            .HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.TrainingQuizQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // TrainingQuizAnswer -> TrainingQuizOption
        modelBuilder.Entity<TrainingQuizAnswer>()
            .HasOne(a => a.SelectedOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.SetNull);

        // TrainingQuiz indexleri
        modelBuilder.Entity<TrainingQuiz>()
            .HasIndex(q => q.TrainingVideoId);
        modelBuilder.Entity<TrainingQuizQuestion>()
            .HasIndex(q => q.TrainingQuizId);
        modelBuilder.Entity<TrainingQuizOption>()
            .HasIndex(o => o.TrainingQuizQuestionId);
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasIndex(r => r.TrainingQuizId);
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasIndex(r => r.TrainingVideoParticipantId);
        modelBuilder.Entity<TrainingQuizResponse>()
            .HasIndex(r => r.TrainingVideoExternalParticipantId);
        modelBuilder.Entity<TrainingQuizAnswer>()
            .HasIndex(a => a.TrainingQuizResponseId);
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


