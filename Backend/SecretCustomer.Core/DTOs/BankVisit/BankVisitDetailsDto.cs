using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.BankVisit;

/// <summary>
/// Banka Ziyaret Detayları DTO (GBF - Gizli Banka Formu)
/// </summary>
public class BankVisitDetailsDto
{
    public Guid Id { get; set; }
    public Guid CustomerVisitId { get; set; }

    // Senaryo Bilgileri
    public BankVisitScenario Scenario { get; set; }
    public string ScenarioName => Scenario.ToString();
    public string? ScenarioDescription { get; set; }
    public bool ScenarioCompleted { get; set; }
    public string? ProductOffered { get; set; }
    public bool CrossSellOffered { get; set; }

    // Zaman Takibi
    public DateTime? EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public int? QueueWaitMinutes { get; set; }
    public int? ServiceDurationMinutes { get; set; }
    public bool QueueTicketTaken { get; set; }
    public string? QueueNumber { get; set; }

    // Personel Değerlendirmesi
    public string? StaffName { get; set; }
    public bool StaffHasNameTag { get; set; }
    public bool GreetingReceived { get; set; }
    public bool FarewellReceived { get; set; }
    public int? StaffAppearanceRating { get; set; }
    public int? StaffKnowledgeRating { get; set; }
    public int? StaffAttentivenessRating { get; set; }
    public int? StaffCommunicationRating { get; set; }
    public int? StaffCountObserved { get; set; }
    public int? BusyCountersCount { get; set; }
    public int? TotalCountersCount { get; set; }

    // Şube Alanları Değerlendirmesi
    public int? EntranceAreaRating { get; set; }
    public int? AtmAreaRating { get; set; }
    public int? WaitingAreaRating { get; set; }
    public int? CounterAreaRating { get; set; }
    public int? ManagerAreaRating { get; set; }

    // Şube Tesisleri
    public int? CleanlinessRating { get; set; }
    public int? LightingRating { get; set; }
    public int? AirConditioningRating { get; set; }
    public int? SignageRating { get; set; }
    public bool BrochuresAvailable { get; set; }
    public bool QueueSystemAvailable { get; set; }
    public bool DisabledAccessAvailable { get; set; }
    public bool SecurityPersonnelPresent { get; set; }

    // ATM Değerlendirmesi
    public int? AtmCount { get; set; }
    public int? WorkingAtmCount { get; set; }
    public int? AtmCleanlinessRating { get; set; }
    public int? AtmUsabilityRating { get; set; }

    // Genel Değerlendirme
    public int? OverallSatisfactionRating { get; set; }
    public int? RecommendationScore { get; set; }
    public bool WouldVisitAgain { get; set; }
    public string? Strengths { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? AdditionalNotes { get; set; }

    // Hesaplanmış Alanlar
    public decimal? AverageStaffRating => CalculateAverageStaffRating();
    public decimal? AverageAreaRating => CalculateAverageAreaRating();
    public decimal? AverageFacilityRating => CalculateAverageFacilityRating();
    public int? TotalTimeInBranch => CalculateTotalTime();

    public DateTime CreatedAt { get; set; }

    private decimal? CalculateAverageStaffRating()
    {
        var ratings = new List<int?> { StaffAppearanceRating, StaffKnowledgeRating, StaffAttentivenessRating, StaffCommunicationRating };
        var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value).ToList();
        return validRatings.Count > 0 ? (decimal)validRatings.Average() : null;
    }

    private decimal? CalculateAverageAreaRating()
    {
        var ratings = new List<int?> { EntranceAreaRating, AtmAreaRating, WaitingAreaRating, CounterAreaRating, ManagerAreaRating };
        var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value).ToList();
        return validRatings.Count > 0 ? (decimal)validRatings.Average() : null;
    }

    private decimal? CalculateAverageFacilityRating()
    {
        var ratings = new List<int?> { CleanlinessRating, LightingRating, AirConditioningRating, SignageRating };
        var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value).ToList();
        return validRatings.Count > 0 ? (decimal)validRatings.Average() : null;
    }

    private int? CalculateTotalTime()
    {
        if (EntryTime.HasValue && ExitTime.HasValue)
        {
            return (int)(ExitTime.Value - EntryTime.Value).TotalMinutes;
        }
        return null;
    }
}

/// <summary>
/// Banka ziyaret detayları oluşturma/güncelleme DTO
/// </summary>
public class CreateBankVisitDetailsDto
{
    public Guid CustomerVisitId { get; set; }

    // Senaryo Bilgileri
    public BankVisitScenario Scenario { get; set; } = BankVisitScenario.GeneralInquiry;
    public string? ScenarioDescription { get; set; }
    public bool ScenarioCompleted { get; set; }
    public string? ProductOffered { get; set; }
    public bool CrossSellOffered { get; set; }

    // Zaman Takibi
    public DateTime? EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public int? QueueWaitMinutes { get; set; }
    public int? ServiceDurationMinutes { get; set; }
    public bool QueueTicketTaken { get; set; }
    public string? QueueNumber { get; set; }

    // Personel Değerlendirmesi
    public string? StaffName { get; set; }
    public bool StaffHasNameTag { get; set; }
    public bool GreetingReceived { get; set; }
    public bool FarewellReceived { get; set; }
    public int? StaffAppearanceRating { get; set; }
    public int? StaffKnowledgeRating { get; set; }
    public int? StaffAttentivenessRating { get; set; }
    public int? StaffCommunicationRating { get; set; }
    public int? StaffCountObserved { get; set; }
    public int? BusyCountersCount { get; set; }
    public int? TotalCountersCount { get; set; }

    // Şube Alanları Değerlendirmesi
    public int? EntranceAreaRating { get; set; }
    public int? AtmAreaRating { get; set; }
    public int? WaitingAreaRating { get; set; }
    public int? CounterAreaRating { get; set; }
    public int? ManagerAreaRating { get; set; }

    // Şube Tesisleri
    public int? CleanlinessRating { get; set; }
    public int? LightingRating { get; set; }
    public int? AirConditioningRating { get; set; }
    public int? SignageRating { get; set; }
    public bool BrochuresAvailable { get; set; }
    public bool QueueSystemAvailable { get; set; }
    public bool DisabledAccessAvailable { get; set; }
    public bool SecurityPersonnelPresent { get; set; }

    // ATM Değerlendirmesi
    public int? AtmCount { get; set; }
    public int? WorkingAtmCount { get; set; }
    public int? AtmCleanlinessRating { get; set; }
    public int? AtmUsabilityRating { get; set; }

    // Genel Değerlendirme
    public int? OverallSatisfactionRating { get; set; }
    public int? RecommendationScore { get; set; }
    public bool WouldVisitAgain { get; set; }
    public string? Strengths { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// Banka ziyareti güncelleme DTO
/// </summary>
public class UpdateBankVisitDetailsDto : CreateBankVisitDetailsDto
{
}

/// <summary>
/// Banka ziyareti özet DTO (liste görünümü için)
/// </summary>
public class BankVisitSummaryDto
{
    public Guid Id { get; set; }
    public Guid CustomerVisitId { get; set; }
    public string? CustomerName { get; set; }
    public string? BranchName { get; set; }
    public string? VisitorName { get; set; }
    public BankVisitScenario Scenario { get; set; }
    public string ScenarioName => GetScenarioDisplayName();
    public bool ScenarioCompleted { get; set; }
    public DateTime? EntryTime { get; set; }
    public int? TotalTimeMinutes { get; set; }
    public int? QueueWaitMinutes { get; set; }
    public int? OverallSatisfactionRating { get; set; }
    public decimal? AverageStaffRating { get; set; }
    public VisitStatus VisitStatus { get; set; }
    public DateTime PlannedDate { get; set; }
    public DateTime CreatedAt { get; set; }

    private string GetScenarioDisplayName()
    {
        return Scenario switch
        {
            BankVisitScenario.GeneralInquiry => "Genel Bilgi Alma",
            BankVisitScenario.AccountOpening => "Hesap Açma",
            BankVisitScenario.LoanApplication => "Kredi Başvurusu",
            BankVisitScenario.CreditCardApplication => "Kredi Kartı Başvurusu",
            BankVisitScenario.DepositTransaction => "Mevduat İşlemi",
            BankVisitScenario.Withdrawal => "Para Çekme",
            BankVisitScenario.MoneyTransfer => "Havale/EFT",
            BankVisitScenario.BillPayment => "Fatura Ödeme",
            BankVisitScenario.CurrencyExchange => "Döviz İşlemi",
            BankVisitScenario.InsuranceInquiry => "Sigorta Danışmanlığı",
            BankVisitScenario.InvestmentInquiry => "Yatırım Danışmanlığı",
            BankVisitScenario.Complaint => "Şikayet",
            BankVisitScenario.UnblockRequest => "Bloke Açma",
            BankVisitScenario.Other => "Diğer",
            _ => Scenario.ToString()
        };
    }
}

/// <summary>
/// Banka ziyareti filtre DTO
/// </summary>
public class BankVisitFilterDto
{
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? VisitorUserId { get; set; }
    public BankVisitScenario? Scenario { get; set; }
    public bool? ScenarioCompleted { get; set; }
    public VisitStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MinSatisfactionRating { get; set; }
    public int? MaxSatisfactionRating { get; set; }
}

/// <summary>
/// Banka ziyareti istatistik DTO
/// </summary>
public class BankVisitStatisticsDto
{
    public int TotalVisits { get; set; }
    public int CompletedScenarios { get; set; }
    public decimal ScenarioCompletionRate { get; set; }
    public decimal AverageWaitTime { get; set; }
    public decimal AverageServiceTime { get; set; }
    public decimal AverageTotalTime { get; set; }
    public decimal AverageStaffRating { get; set; }
    public decimal AverageAreaRating { get; set; }
    public decimal AverageFacilityRating { get; set; }
    public decimal AverageOverallSatisfaction { get; set; }
    public decimal AverageRecommendationScore { get; set; }
    public int GreetingReceivedCount { get; set; }
    public int FarewellReceivedCount { get; set; }
    public decimal GreetingRate { get; set; }
    public decimal FarewellRate { get; set; }
    public Dictionary<BankVisitScenario, int> ScenarioDistribution { get; set; } = new();
    public Dictionary<int, int> SatisfactionDistribution { get; set; } = new();
}
