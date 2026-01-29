using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Email şablonu - Anket davetiyeleri, bildirimler vb. için
/// </summary>
public class EmailTemplate : BaseEntity
{
    /// <summary>
    /// Şablon adı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Şablon açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Email konusu (Subject)
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email içeriği (HTML)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Şablon tipi
    /// </summary>
    public int TemplateTypeId { get; set; } = EmailTemplateTypes.Ids.SurveyInvitation;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Varsayılan şablon mu?
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Müşteriye özel mi? (null ise genel şablon)
    /// </summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}

/// <summary>
/// Email şablonu placeholder'ları
/// </summary>
public static class EmailPlaceholders
{
    // Anket Linkleri
    public const string SurveyLink = "{SurveyLink}";
    public const string SurveyUrl = "{SurveyUrl}";
    public const string SurveyQRCode = "{SurveyQRCode}";

    // Firma/Organizasyon
    public const string CompanyName = "{CompanyName}";
    public const string OrganizationName = "{OrganizationName}";

    // Alıcı Bilgileri
    public const string RecipientName = "{RecipientName}";
    public const string RecipientFirstName = "{RecipientFirstName}";
    public const string RecipientLastName = "{RecipientLastName}";
    public const string RecipientEmail = "{RecipientEmail}";

    // Proje/Anket Bilgileri
    public const string ProjectName = "{ProjectName}";
    public const string SurveyName = "{SurveyName}";
    public const string DueDate = "{DueDate}";
    public const string StartDate = "{StartDate}";
    public const string EndDate = "{EndDate}";

    // Değerlendirme Bilgileri
    public const string EvaluationScore = "{EvaluationScore}";
    public const string EvaluationScorePercentage = "{EvaluationScorePercentage}";
    public const string EvaluatedPersonnel = "{EvaluatedPersonnel}";
    public const string EvaluationDate = "{EvaluationDate}";
    public const string EvaluationCount = "{EvaluationCount}";
    public const string EvaluationSummaryTable = "{EvaluationSummaryTable}";
    public const string PeriodStartDate = "{PeriodStartDate}";
    public const string PeriodEndDate = "{PeriodEndDate}";

    // Sistem
    public const string CurrentDate = "{CurrentDate}";
    public const string CurrentYear = "{CurrentYear}";
    public const string SystemName = "{SystemName}";

    // Eğitim Videosu
    public const string TrainingVideoTitle = "{TrainingVideoTitle}";
    public const string TrainingVideoDescription = "{TrainingVideoDescription}";
    public const string TrainingVideoDuration = "{TrainingVideoDuration}";
    public const string TrainingVideoLink = "{TrainingVideoLink}";
    public const string TrainingVideoUrl = "{TrainingVideoUrl}";
    public const string TrainingAssignmentTitle = "{TrainingAssignmentTitle}";
    public const string TrainingDueDate = "{TrainingDueDate}";
    public const string TrainingStartDate = "{TrainingStartDate}";

    /// <summary>
    /// Tüm placeholder'ları kategorili listele
    /// </summary>
    public static List<PlaceholderCategory> GetAllCategories()
    {
        return new List<PlaceholderCategory>
        {
            new PlaceholderCategory
            {
                Name = "Anket Linkleri",
                Icon = "bi-link-45deg",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(SurveyLink, "Anket Linki (Hazır)", "Tıklanabilir link olarak gelir"),
                    new(SurveyUrl, "Anket URL", "Sadece URL - kendi linkinizi oluşturmak için"),
                    new(SurveyQRCode, "QR Kod", "Anket QR kodu (img tag olarak)")
                }
            },
            new PlaceholderCategory
            {
                Name = "Firma Bilgileri",
                Icon = "bi-building",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(CompanyName, "Firma Adı", "Müşteri firma adı"),
                    new(OrganizationName, "Organizasyon/Şube", "Organizasyon veya şube adı")
                }
            },
            new PlaceholderCategory
            {
                Name = "Alıcı Bilgileri",
                Icon = "bi-person",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(RecipientName, "Ad Soyad", "Alıcının tam adı"),
                    new(RecipientFirstName, "Ad", "Alıcının adı"),
                    new(RecipientLastName, "Soyad", "Alıcının soyadı"),
                    new(RecipientEmail, "Email", "Alıcının email adresi")
                }
            },
            new PlaceholderCategory
            {
                Name = "Proje/Anket",
                Icon = "bi-clipboard-data",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(ProjectName, "Proje Adı", "Proje adı"),
                    new(SurveyName, "Anket Adı", "Anket/Checklist adı"),
                    new(DueDate, "Son Tarih", "Anketin bitiş tarihi"),
                    new(StartDate, "Başlangıç", "Projenin başlangıç tarihi"),
                    new(EndDate, "Bitiş", "Projenin bitiş tarihi")
                }
            },
            new PlaceholderCategory
            {
                Name = "Değerlendirme",
                Icon = "bi-clipboard-check",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(EvaluationScore, "Puan", "Değerlendirme puanı"),
                    new(EvaluationScorePercentage, "Puan (%)", "Değerlendirme yüzdesi"),
                    new(EvaluatedPersonnel, "Değerlendirilen", "Değerlendirilen personel adı"),
                    new(EvaluationDate, "Değerlendirme Tarihi", "Değerlendirmenin yapıldığı tarih"),
                    new(EvaluationCount, "Değerlendirme Sayısı", "Dönem içi toplam değerlendirme sayısı"),
                    new(EvaluationSummaryTable, "Özet Tablo", "Dönem değerlendirme özet tablosu (HTML)"),
                    new(PeriodStartDate, "Dönem Başlangıç", "Bildirim dönemi başlangıç tarihi"),
                    new(PeriodEndDate, "Dönem Bitiş", "Bildirim dönemi bitiş tarihi")
                }
            },
            new PlaceholderCategory
            {
                Name = "Sistem",
                Icon = "bi-gear",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(CurrentDate, "Bugün", "Bugünün tarihi"),
                    new(CurrentYear, "Yıl", "Mevcut yıl"),
                    new(SystemName, "Sistem Adı", "Uygulama adı")
                }
            },
            new PlaceholderCategory
            {
                Name = "Eğitim Videosu",
                Icon = "bi-play-circle",
                Placeholders = new List<PlaceholderInfo>
                {
                    new(TrainingVideoTitle, "Video Başlığı", "Eğitim videosu başlığı"),
                    new(TrainingVideoDescription, "Video Açıklaması", "Video açıklaması"),
                    new(TrainingVideoDuration, "Video Süresi", "Video süresi (örn: 5 dk 30 sn)"),
                    new(TrainingVideoLink, "Video Linki", "Videoyu izleme linki (HTML anchor)"),
                    new(TrainingVideoUrl, "Video URL", "Videoyu izleme URL'si (sadece adres)"),
                    new(TrainingAssignmentTitle, "Atama Başlığı", "Eğitim ataması başlığı"),
                    new(TrainingStartDate, "Başlangıç Tarihi", "Eğitim başlangıç tarihi"),
                    new(TrainingDueDate, "Son Tarih", "Eğitim son tarihi")
                }
            }
        };
    }
}

public class PlaceholderCategory
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<PlaceholderInfo> Placeholders { get; set; } = new();
}

public class PlaceholderInfo
{
    public string Code { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public PlaceholderInfo(string code, string label, string description)
    {
        Code = code;
        Label = label;
        Description = description;
    }
}
