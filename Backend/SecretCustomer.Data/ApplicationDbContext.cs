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
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Evaluation> Evaluations { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<FieldWorker> FieldWorkers { get; set; }
    public DbSet<ExcelTemplate> ExcelTemplates { get; set; }
    public DbSet<ExcelColumn> ExcelColumns { get; set; }
    
    // Customer Management DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerPersonnel> CustomerPersonnel { get; set; }
    public DbSet<CustomerTaskList> CustomerTaskLists { get; set; }
    public DbSet<CustomerPersonnelTaskAssignment> CustomerPersonnelTaskAssignments { get; set; }
    public DbSet<CustomerPersonnelPermission> CustomerPersonnelPermissions { get; set; }

    // Permission Management DbSets
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    // Project Management DbSets
    public DbSet<ProjectBranch> ProjectBranches { get; set; }
    public DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; }

    // Personnel Management DbSets
    public DbSet<Personnel> Personnel { get; set; }

    // Organization Management DbSets
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<Delegation> Delegations { get; set; }

    // Question Attachments
    public DbSet<QuestionAttachment> QuestionAttachments { get; set; }

    // Announcements
    public DbSet<Announcement> Announcements { get; set; }

    // Customer Visits
    public DbSet<CustomerVisit> CustomerVisits { get; set; }
    public DbSet<CustomerVisitAttachment> CustomerVisitAttachments { get; set; }

    // Meetings
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
    public DbSet<MeetingAttachment> MeetingAttachments { get; set; }

    // Calls
    public DbSet<Call> Calls { get; set; }
    public DbSet<CallAttachment> CallAttachments { get; set; }

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

    // Visit Detail System (Dinamik alan sistemi)
    public DbSet<VisitSector> VisitSectors { get; set; }
    public DbSet<VisitFieldDefinition> VisitFieldDefinitions { get; set; }
    public DbSet<VisitDetailValue> VisitDetailValues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Branch>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Checklist>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Section>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Question>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Assignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Evaluation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Answer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FieldWorker>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExcelTemplate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExcelColumn>().HasQueryFilter(e => !e.IsDeleted);
        
        // Customer Management Entities
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnel>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerTaskList>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelTaskAssignment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerPersonnelPermission>().HasQueryFilter(e => !e.IsDeleted);

        // Permission Management Entities
        modelBuilder.Entity<Permission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(e => !e.IsDeleted);

        // Project Management Entities
        modelBuilder.Entity<ProjectBranch>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProjectTeamMember>().HasQueryFilter(e => !e.IsDeleted);

        // Personnel Management Entities
        modelBuilder.Entity<Personnel>().HasQueryFilter(e => !e.IsDeleted);

        // Organization Management Entities
        modelBuilder.Entity<OrganizationUnit>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Delegation>().HasQueryFilter(e => !e.IsDeleted);

        // Question Attachments
        modelBuilder.Entity<QuestionAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Announcements
        modelBuilder.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted);

        // Customer Visits
        modelBuilder.Entity<CustomerVisit>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerVisitAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Meetings
        modelBuilder.Entity<Meeting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MeetingParticipant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MeetingAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Calls
        modelBuilder.Entity<Call>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CallAttachment>().HasQueryFilter(e => !e.IsDeleted);

        // Trainings
        modelBuilder.Entity<Training>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingParticipant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TrainingMaterial>().HasQueryFilter(e => !e.IsDeleted);

        // Approvals and Notifications
        modelBuilder.Entity<Approval>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NotificationSetting>().HasQueryFilter(e => !e.IsDeleted);

        // Visit Detail System
        modelBuilder.Entity<VisitSector>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<VisitFieldDefinition>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<VisitDetailValue>().HasQueryFilter(e => !e.IsDeleted);

        // Delegation relationships configuration
        modelBuilder.Entity<Delegation>()
            .HasOne(d => d.DelegatorUser)
            .WithMany(u => u.DelegationsGiven)
            .HasForeignKey(d => d.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delegation>()
            .HasOne(d => d.DelegateeUser)
            .WithMany(u => u.DelegationsReceived)
            .HasForeignKey(d => d.DelegateeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delegation>()
            .HasOne(d => d.ApprovedByUser)
            .WithMany(u => u.DelegationsApproved)
            .HasForeignKey(d => d.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delegation>()
            .HasOne(d => d.DelegatorOrganizationUnit)
            .WithMany(o => o.DelegationsFrom)
            .HasForeignKey(d => d.DelegatorOrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delegation>()
            .HasOne(d => d.DelegateeOrganizationUnit)
            .WithMany(o => o.DelegationsTo)
            .HasForeignKey(d => d.DelegateeOrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrganizationUnit self-referencing relationship
        modelBuilder.Entity<OrganizationUnit>()
            .HasOne(o => o.Parent)
            .WithMany(o => o.Children)
            .HasForeignKey(o => o.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrganizationUnit Manager relationship
        modelBuilder.Entity<OrganizationUnit>()
            .HasOne(o => o.Manager)
            .WithMany(u => u.ManagedOrganizationUnits)
            .HasForeignKey(o => o.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // User OrganizationUnit relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.OrganizationUnit)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationUnitId)
            .OnDelete(DeleteBehavior.SetNull);
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


