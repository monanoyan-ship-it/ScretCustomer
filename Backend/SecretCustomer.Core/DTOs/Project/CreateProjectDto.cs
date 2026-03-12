using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Project;

public class CreateProjectDto
{
    public string? Code { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int ChecklistId { get; set; }

    [Required]
    public string ProjectType { get; set; } = "MysteryShopping";

    [Required]
    public string AssignmentType { get; set; } = "InternalBranch";

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Hedefler
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }

    // Butce
    public decimal? EstimatedBudget { get; set; }
    public decimal? CostPerEvaluation { get; set; }

    // Raporlama
    public int? ReportingFrequencyDays { get; set; }
    public bool AutoGenerateReports { get; set; } = false;

    // Yonetici
    public int? ProjectManagerId { get; set; }

    // Diger
    public decimal? MinimumScoreThreshold { get; set; }
    public string? Priority { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }

    // Musteri
    public int? CustomerId { get; set; }

    // Organizasyon/Sube (null ise tum firma)
    public int? OrganizationId { get; set; }

    // ===== ANKET AYARLARI (Survey Settings) =====
    // Sadece ProjectType = "OnlineSurvey" olduğunda kullanılır

    /// <summary>
    /// Anket kimlik tipi: "Anonymous" veya "Identified"
    /// </summary>
    public string? SurveyIdentityType { get; set; }

    /// <summary>
    /// Anket davetiye email şablonu ID
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Hatırlatma email şablonu ID
    /// </summary>
    public int? ReminderEmailTemplateId { get; set; }

    /// <summary>
    /// Hatırlatma mail gönderilsin mi?
    /// </summary>
    public bool SendReminderEmails { get; set; } = false;

    /// <summary>
    /// Hatırlatma gün sayısı (bitiş tarihinden X gün önce)
    /// </summary>
    public int? ReminderDaysBeforeEnd { get; set; }

    // ===== DEĞERLENDİRME AYARLARI (Assessment Settings) =====
    // Sadece ProjectType = "PersonnelAssessment" olduğunda kullanılır

    /// <summary>
    /// Değerlendirme modu: 1=180°, 2=360°
    /// </summary>
    public int? AssessmentModeId { get; set; }

    /// <summary>
    /// Anonim değerlendirme (Peer/Subordinate toplu form)
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// Raporda sonuç göstermek için min. katılımcı sayısı
    /// </summary>
    public int MinRespondentsForAnonymity { get; set; } = 2;

    // Takim Uyeleri
    public List<CreateProjectTeamMemberDto>? TeamMembers { get; set; }
}

public class CreateProjectTeamMemberDto
{
    [Required]
    public int UserId { get; set; }
    public string Role { get; set; } = "Evaluator";
}

/// <summary>
/// Proje durumu guncelleme
/// </summary>
public class UpdateProjectStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Proje takimina uye ekleme/cikarma
/// </summary>
public class ManageProjectTeamDto
{
    public List<int> AddUserIds { get; set; } = new();
    public List<int> RemoveUserIds { get; set; } = new();
}

