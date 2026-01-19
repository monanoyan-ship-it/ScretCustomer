namespace SecretCustomer.Core.DTOs.AI;

/// <summary>
/// AI Rapor Oluşturma İstek DTO
/// </summary>
public class AIReportRequestDto
{
    /// <summary>
    /// Müşteri ID
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Rapor yılı (2025 gibi)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Karşılaştırma yılı (opsiyonel, örn: 2024)
    /// </summary>
    public int? CompareYear { get; set; }

    /// <summary>
    /// Rapor dili (tr, en)
    /// </summary>
    public string Language { get; set; } = "tr";

    /// <summary>
    /// Hedef puanlar
    /// </summary>
    public AITargetScoresDto TargetScores { get; set; } = new();
}

/// <summary>
/// Hedef puanlar DTO
/// </summary>
public class AITargetScoresDto
{
    /// <summary>
    /// Önceki yıl hedefi (örn: 2024 için 85)
    /// </summary>
    public decimal PreviousYearTarget { get; set; } = 85;

    /// <summary>
    /// Mevcut yıl hedefi (örn: 2025 için 90)
    /// </summary>
    public decimal CurrentYearTarget { get; set; } = 90;

    /// <summary>
    /// Üst hedef (örn: 95)
    /// </summary>
    public decimal StretchTarget { get; set; } = 95;
}

/// <summary>
/// AI Rapor Yanıt DTO
/// </summary>
public class AIReportResponseDto
{
    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// AI tarafından üretilen rapor içeriği (Markdown formatında)
    /// </summary>
    public string? ReportContent { get; set; }

    /// <summary>
    /// Kullanılan token sayısı
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// AI'ya gönderilecek rapor verisi DTO
/// </summary>
public class AIReportDataDto
{
    /// <summary>
    /// Müşteri bilgileri
    /// </summary>
    public AICustomerInfoDto Customer { get; set; } = new();

    /// <summary>
    /// Hedef puanlar
    /// </summary>
    public AITargetScoresDto TargetScores { get; set; } = new();

    /// <summary>
    /// Yönetici özeti - Yıl bazlı genel ortalamalar
    /// </summary>
    public AIExecutiveSummaryDto ExecutiveSummary { get; set; } = new();

    /// <summary>
    /// Proje bazlı aylık puanlar (Inbound projeleri)
    /// </summary>
    public List<AIProjectMonthlyDto> InboundProjects { get; set; } = new();

    /// <summary>
    /// Proje bazlı aylık puanlar (Outbound projeleri)
    /// </summary>
    public List<AIProjectMonthlyDto> OutboundProjects { get; set; } = new();

    /// <summary>
    /// Kriter bazlı karşılaştırma (Inbound vs Outbound)
    /// </summary>
    public List<AICriteriaComparisonDto> CriteriaComparison { get; set; } = new();

    /// <summary>
    /// Personel sıralaması (tüm personeller)
    /// </summary>
    public List<AIPersonnelRankingDto> PersonnelRanking { get; set; } = new();

    /// <summary>
    /// Personel bazlı aylık puanlar (detay)
    /// </summary>
    public List<AIPersonnelMonthlyDto> PersonnelMonthlyScores { get; set; } = new();
}

/// <summary>
/// Müşteri bilgisi
/// </summary>
public class AICustomerInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>
/// Yönetici Özeti - Yıl bazlı karşılaştırma
/// </summary>
public class AIExecutiveSummaryDto
{
    public int CurrentYear { get; set; }
    public int? CompareYear { get; set; }

    /// <summary>
    /// Mevcut yıl genel ortalaması
    /// </summary>
    public decimal CurrentYearAverage { get; set; }

    /// <summary>
    /// Karşılaştırma yılı genel ortalaması
    /// </summary>
    public decimal? CompareYearAverage { get; set; }

    /// <summary>
    /// Toplam dinlenen çağrı sayısı (mevcut yıl)
    /// </summary>
    public int TotalEvaluations { get; set; }

    /// <summary>
    /// Proje bazlı yıllık ortalamalar
    /// </summary>
    public List<AIProjectYearlySummaryDto> ProjectSummaries { get; set; } = new();
}

/// <summary>
/// Proje bazlı yıllık özet
/// </summary>
public class AIProjectYearlySummaryDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
    public int TotalEvaluations { get; set; }
    public decimal CurrentYearAverage { get; set; }
    public decimal? CompareYearAverage { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Proje bazlı aylık puanlar
/// </summary>
public class AIProjectMonthlyDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
    public int TotalEvaluations { get; set; }

    /// <summary>
    /// Önceki yıl ortalaması
    /// </summary>
    public decimal? PreviousYearAverage { get; set; }

    /// <summary>
    /// Mevcut yıl ortalaması
    /// </summary>
    public decimal CurrentYearAverage { get; set; }

    /// <summary>
    /// Aylık puanlar (Ocak=1, Aralık=12)
    /// </summary>
    public Dictionary<int, decimal?> MonthlyScores { get; set; } = new();
}

/// <summary>
/// Kriter bazlı Inbound vs Outbound karşılaştırma
/// </summary>
public class AICriteriaComparisonDto
{
    /// <summary>
    /// Kriter adı (soru metni)
    /// </summary>
    public string CriteriaName { get; set; } = string.Empty;

    /// <summary>
    /// Inbound ortalaması
    /// </summary>
    public decimal InboundScore { get; set; }

    /// <summary>
    /// Outbound ortalaması
    /// </summary>
    public decimal OutboundScore { get; set; }

    /// <summary>
    /// Yorum/Analiz (AI tarafından doldurulacak)
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Personel sıralaması
/// </summary>
public class AIPersonnelRankingDto
{
    public string PersonnelName { get; set; } = string.Empty;
    public string? ProjectGroup { get; set; }

    /// <summary>
    /// Yıllık ortalama puan
    /// </summary>
    public decimal AverageScore { get; set; }

    /// <summary>
    /// Dinlenen çağrı sayısı
    /// </summary>
    public int EvaluationCount { get; set; }
}

/// <summary>
/// Personel bazlı aylık puanlar
/// </summary>
public class AIPersonnelMonthlyDto
{
    public string PersonnelName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;

    /// <summary>
    /// Aylık puanlar (Ocak=1, Aralık=12)
    /// </summary>
    public Dictionary<int, decimal?> MonthlyScores { get; set; } = new();
}
