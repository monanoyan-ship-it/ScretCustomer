using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Banka Gizli Müşteri Ziyaret Detayları (GBF - Gizli Banka Formu)
/// CustomerVisit entity'sine bağlı olarak banka ziyaretine özel alanları tutar
/// </summary>
public class BankVisitDetails : BaseEntity
{
    /// <summary>
    /// İlişkili müşteri ziyareti
    /// </summary>
    public Guid CustomerVisitId { get; set; }
    public CustomerVisit CustomerVisit { get; set; } = null!;

    // ===== SENARYO BİLGİLERİ =====

    /// <summary>
    /// Ziyaret senaryosu
    /// </summary>
    public BankVisitScenario Scenario { get; set; } = BankVisitScenario.GeneralInquiry;

    /// <summary>
    /// Senaryo detayı/açıklaması
    /// </summary>
    public string? ScenarioDescription { get; set; }

    /// <summary>
    /// Senaryo tamamlandı mı?
    /// </summary>
    public bool ScenarioCompleted { get; set; }

    /// <summary>
    /// Teklif edilen ürün/hizmet
    /// </summary>
    public string? ProductOffered { get; set; }

    /// <summary>
    /// Çapraz satış yapıldı mı?
    /// </summary>
    public bool CrossSellOffered { get; set; }

    // ===== ZAMAN TAKİBİ =====

    /// <summary>
    /// Şubeye giriş zamanı
    /// </summary>
    public DateTime? EntryTime { get; set; }

    /// <summary>
    /// Şubeden çıkış zamanı
    /// </summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>
    /// Sıra bekleme süresi (dakika)
    /// </summary>
    public int? QueueWaitMinutes { get; set; }

    /// <summary>
    /// Hizmet alma süresi (dakika)
    /// </summary>
    public int? ServiceDurationMinutes { get; set; }

    /// <summary>
    /// Sıra numarası alındı mı?
    /// </summary>
    public bool QueueTicketTaken { get; set; }

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public string? QueueNumber { get; set; }

    // ===== PERSONEL DEĞERLENDİRMESİ =====

    /// <summary>
    /// Görüşülen personel adı
    /// </summary>
    public string? StaffName { get; set; }

    /// <summary>
    /// Personel yaka kartı var mı?
    /// </summary>
    public bool StaffHasNameTag { get; set; }

    /// <summary>
    /// Karşılama yapıldı mı?
    /// </summary>
    public bool GreetingReceived { get; set; }

    /// <summary>
    /// Vedalaşma yapıldı mı?
    /// </summary>
    public bool FarewellReceived { get; set; }

    /// <summary>
    /// Personel dış görünüm puanı (1-5)
    /// </summary>
    public int? StaffAppearanceRating { get; set; }

    /// <summary>
    /// Personel bilgi düzeyi puanı (1-5)
    /// </summary>
    public int? StaffKnowledgeRating { get; set; }

    /// <summary>
    /// Personel ilgi/ilgilenme puanı (1-5)
    /// </summary>
    public int? StaffAttentivenessRating { get; set; }

    /// <summary>
    /// Personel iletişim puanı (1-5)
    /// </summary>
    public int? StaffCommunicationRating { get; set; }

    /// <summary>
    /// Gözlemlenen personel sayısı
    /// </summary>
    public int? StaffCountObserved { get; set; }

    /// <summary>
    /// Meşgul gişe sayısı
    /// </summary>
    public int? BusyCountersCount { get; set; }

    /// <summary>
    /// Toplam gişe sayısı
    /// </summary>
    public int? TotalCountersCount { get; set; }

    // ===== ŞUBE ALANLARI DEĞERLENDİRMESİ =====

    /// <summary>
    /// Şube girişi puanı (1-5)
    /// </summary>
    public int? EntranceAreaRating { get; set; }

    /// <summary>
    /// ATM alanı puanı (1-5)
    /// </summary>
    public int? AtmAreaRating { get; set; }

    /// <summary>
    /// Bekleme alanı puanı (1-5)
    /// </summary>
    public int? WaitingAreaRating { get; set; }

    /// <summary>
    /// Gişe alanı puanı (1-5)
    /// </summary>
    public int? CounterAreaRating { get; set; }

    /// <summary>
    /// Müdür/Yetkili odası puanı (1-5)
    /// </summary>
    public int? ManagerAreaRating { get; set; }

    // ===== ŞUBE TESİSLERİ =====

    /// <summary>
    /// Genel temizlik puanı (1-5)
    /// </summary>
    public int? CleanlinessRating { get; set; }

    /// <summary>
    /// Aydınlatma puanı (1-5)
    /// </summary>
    public int? LightingRating { get; set; }

    /// <summary>
    /// Havalandırma/Klima puanı (1-5)
    /// </summary>
    public int? AirConditioningRating { get; set; }

    /// <summary>
    /// Tabela/Yönlendirme puanı (1-5)
    /// </summary>
    public int? SignageRating { get; set; }

    /// <summary>
    /// Broşür/Reklam malzemesi mevcut mu?
    /// </summary>
    public bool BrochuresAvailable { get; set; }

    /// <summary>
    /// Sıra sistemi mevcut mu?
    /// </summary>
    public bool QueueSystemAvailable { get; set; }

    /// <summary>
    /// Engelli erişimi uygun mu?
    /// </summary>
    public bool DisabledAccessAvailable { get; set; }

    /// <summary>
    /// Güvenlik personeli mevcut mu?
    /// </summary>
    public bool SecurityPersonnelPresent { get; set; }

    // ===== ATM DEĞERLENDİRMESİ =====

    /// <summary>
    /// ATM sayısı
    /// </summary>
    public int? AtmCount { get; set; }

    /// <summary>
    /// Çalışan ATM sayısı
    /// </summary>
    public int? WorkingAtmCount { get; set; }

    /// <summary>
    /// ATM temizlik puanı (1-5)
    /// </summary>
    public int? AtmCleanlinessRating { get; set; }

    /// <summary>
    /// ATM kullanım kolaylığı puanı (1-5)
    /// </summary>
    public int? AtmUsabilityRating { get; set; }

    // ===== GENEL DEĞERLENDİRME =====

    /// <summary>
    /// Genel memnuniyet puanı (1-10)
    /// </summary>
    public int? OverallSatisfactionRating { get; set; }

    /// <summary>
    /// Bu şubeyi tavsiye eder misiniz? (1-10)
    /// </summary>
    public int? RecommendationScore { get; set; }

    /// <summary>
    /// Tekrar ziyaret eder misiniz?
    /// </summary>
    public bool WouldVisitAgain { get; set; }

    /// <summary>
    /// Güçlü yönler
    /// </summary>
    public string? Strengths { get; set; }

    /// <summary>
    /// Gelişim alanları
    /// </summary>
    public string? ImprovementAreas { get; set; }

    /// <summary>
    /// Ek notlar
    /// </summary>
    public string? AdditionalNotes { get; set; }
}

/// <summary>
/// Banka ziyaret senaryoları
/// </summary>
public enum BankVisitScenario
{
    /// <summary>
    /// Genel bilgi alma
    /// </summary>
    GeneralInquiry = 0,

    /// <summary>
    /// Hesap açma
    /// </summary>
    AccountOpening = 1,

    /// <summary>
    /// Kredi başvurusu
    /// </summary>
    LoanApplication = 2,

    /// <summary>
    /// Kredi kartı başvurusu
    /// </summary>
    CreditCardApplication = 3,

    /// <summary>
    /// Mevduat işlemi
    /// </summary>
    DepositTransaction = 4,

    /// <summary>
    /// Para çekme
    /// </summary>
    Withdrawal = 5,

    /// <summary>
    /// Havale/EFT
    /// </summary>
    MoneyTransfer = 6,

    /// <summary>
    /// Fatura ödeme
    /// </summary>
    BillPayment = 7,

    /// <summary>
    /// Döviz işlemi
    /// </summary>
    CurrencyExchange = 8,

    /// <summary>
    /// Sigorta danışmanlığı
    /// </summary>
    InsuranceInquiry = 9,

    /// <summary>
    /// Yatırım danışmanlığı
    /// </summary>
    InvestmentInquiry = 10,

    /// <summary>
    /// Şikayet bildirimi
    /// </summary>
    Complaint = 11,

    /// <summary>
    /// Kart/Hesap bloke açma
    /// </summary>
    UnblockRequest = 12,

    /// <summary>
    /// Diğer
    /// </summary>
    Other = 99
}
