using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Proje", Description = "Projeler icin Excel import/export", IsAvailable = true)]
public class Project : BaseEntity
{
    [ExcelColumn("Proje Kodu", 1, ColumnType = ExcelColumnType.Text,
        Description = "Proje kodu", SampleValue = "PRJ-2025-001")]
    public string? Code { get; set; }

    [ExcelColumn("Proje Adi", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Projenin adi", SampleValue = "2024 Musteri Memnuniyeti Projesi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Aciklama", 3, ColumnType = ExcelColumnType.Text,
        Description = "Proje hakkinda detayli aciklama", SampleValue = "Q1 donemi musteri memnuniyeti olcumu")]
    public string? Description { get; set; }

    public int ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    [ExcelColumn("Proje Tipi", 4, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Proje tipi",
        DropdownOptions = "[\"MysteryShopping\", \"CallAuditing\", \"PhysicalAudit\", \"OnlineSurvey\", \"CustomerSatisfaction\", \"TrainingEvaluation\", \"QualityControl\"]",
        SampleValue = "MysteryShopping")]
    public ProjectType ProjectType { get; set; } = ProjectType.MysteryShopping;

    [ExcelColumn("Proje Durumu", 5, ColumnType = ExcelColumnType.Dropdown,
        Description = "Proje durumu",
        DropdownOptions = "[\"Draft\", \"Planned\", \"Active\", \"Paused\", \"Completed\", \"Cancelled\"]",
        SampleValue = "Active")]
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    [ExcelColumn("Atama Tipi", 6, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Atama tipi",
        DropdownOptions = "[\"InternalBranch\", \"InternalUser\", \"ExternalCustomer\", \"CustomerPersonnel\", \"FieldWorker\"]",
        SampleValue = "InternalBranch")]
    public AssignmentType AssignmentType { get; set; }

    [ExcelColumn("Baslangic Tarihi", 7, IsRequired = true, ColumnType = ExcelColumnType.Date,
        Description = "Projenin baslangic tarihi", SampleValue = "2024-01-01")]
    public DateTime StartDate { get; set; }

    [ExcelColumn("Bitis Tarihi", 8, IsRequired = true, ColumnType = ExcelColumnType.Date,
        Description = "Projenin bitis tarihi", SampleValue = "2024-12-31")]
    public DateTime EndDate { get; set; }

    [ExcelColumn("Aktif", 9, ColumnType = ExcelColumnType.Boolean,
        Description = "Projenin aktif olup olmadigi", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // ===== YENI ALANLAR - Proje Yonetimi Gelistirmeleri =====

    /// <summary>
    /// Hedef degerlendirme sayisi
    /// </summary>
    [ExcelColumn("Hedef Sayi", 10, ColumnType = ExcelColumnType.Number,
        Description = "Hedef degerlendirme sayisi", SampleValue = "100")]
    public int? TargetCount { get; set; }

    /// <summary>
    /// Gunluk kota
    /// </summary>
    [ExcelColumn("Gunluk Kota", 11, ColumnType = ExcelColumnType.Number,
        Description = "Gunluk maksimum degerlendirme sayisi")]
    public int? DailyQuota { get; set; }

    /// <summary>
    /// Haftalik kota
    /// </summary>
    [ExcelColumn("Haftalik Kota", 12, ColumnType = ExcelColumnType.Number,
        Description = "Haftalik maksimum degerlendirme sayisi")]
    public int? WeeklyQuota { get; set; }

    /// <summary>
    /// Aylik kota
    /// </summary>
    [ExcelColumn("Aylik Kota", 13, ColumnType = ExcelColumnType.Number,
        Description = "Aylik maksimum degerlendirme sayisi")]
    public int? MonthlyQuota { get; set; }

    /// <summary>
    /// Tahmini butce
    /// </summary>
    [ExcelColumn("Tahmini Butce", 14, ColumnType = ExcelColumnType.Number,
        Description = "Tahmini proje butcesi", SampleValue = "50000")]
    public decimal? EstimatedBudget { get; set; }

    /// <summary>
    /// Gerceklesen butce
    /// </summary>
    [ExcelColumn("Gerceklesen Butce", 15, ColumnType = ExcelColumnType.Number,
        Description = "Gerceklesen proje butcesi")]
    public decimal? ActualBudget { get; set; }

    /// <summary>
    /// Degerlendirme basina ucret
    /// </summary>
    [ExcelColumn("Birim Ucret", 16, ColumnType = ExcelColumnType.Number,
        Description = "Degerlendirme basina ucret", SampleValue = "150")]
    public decimal? CostPerEvaluation { get; set; }

    /// <summary>
    /// Raporlama sikligi (gun)
    /// </summary>
    [ExcelColumn("Raporlama Sikligi", 17, ColumnType = ExcelColumnType.Number,
        Description = "Raporlama sikligi (gun)", SampleValue = "7")]
    public int? ReportingFrequencyDays { get; set; }

    /// <summary>
    /// Otomatik rapor olusturma
    /// </summary>
    [ExcelColumn("Otomatik Rapor", 18, ColumnType = ExcelColumnType.Boolean,
        Description = "Otomatik rapor olusturulsun mu?", SampleValue = "true")]
    public bool AutoGenerateReports { get; set; } = false;

    /// <summary>
    /// Son rapor tarihi
    /// </summary>
    public DateTime? LastReportDate { get; set; }

    /// <summary>
    /// Proje yoneticisi ID
    /// </summary>
    public int? ProjectManagerId { get; set; }
    public User? ProjectManager { get; set; }

    /// <summary>
    /// Minimum puan esigi (basari icin)
    /// </summary>
    [ExcelColumn("Minimum Puan", 19, ColumnType = ExcelColumnType.Number,
        Description = "Basari icin minimum puan esigi", SampleValue = "70")]
    public decimal? MinimumScoreThreshold { get; set; }

    /// <summary>
    /// Proje onceligi
    /// </summary>
    [ExcelColumn("Oncelik", 20, ColumnType = ExcelColumnType.Dropdown,
        Description = "Proje onceligi",
        DropdownOptions = "[\"Low\", \"Medium\", \"High\", \"Critical\"]",
        SampleValue = "Medium")]
    public string? Priority { get; set; }

    /// <summary>
    /// Etiketler (virgul ile ayrilmis)
    /// </summary>
    [ExcelColumn("Etiketler", 21, ColumnType = ExcelColumnType.Text,
        Description = "Proje etiketleri", SampleValue = "Q1,Istanbul,Perakende")]
    public string? Tags { get; set; }

    /// <summary>
    /// Proje notlari
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Proje takimi (atanan kullanicilar)
    /// </summary>
    public ICollection<ProjectTeamMember> TeamMembers { get; set; } = new List<ProjectTeamMember>();

    /// <summary>
    /// Proje dosyalari
    /// </summary>
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
}

/// <summary>
/// Proje takimi uyesi
/// </summary>
public class ProjectTeamMember : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Rol (Yonetici, Denetci, Gozlemci vb.)
    /// </summary>
    public string Role { get; set; } = "Evaluator";

    /// <summary>
    /// Atama tarihi
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}
