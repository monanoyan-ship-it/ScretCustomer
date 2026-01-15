# AI Rapor Yorumlama Modülü - Analiz ve Planlama

> **Son Güncelleme:** Ocak 2026
> **Durum:** PLANLANMIŞ (Henüz implemente edilmedi)

## Özet

Firma değerlendirme raporlarını yapay zeka (OpenAI GPT-5.2) ile yorumlatma sistemi. Kullanıcı rapor datasını gönderir, AI analiz edip anlamlı yorumlar üretir.

---

## Teknik Kararlar

| Karar | Seçim | Gerekçe |
|-------|-------|---------|
| AI Provider | OpenAI GPT-5.2 | Türkçe'de en iyi performans, stabil API |
| Data Format | JSON | Az token, net parse |
| Prompt Dili | İngilizce | Daha az token, tutarlı sonuç |
| Çıktı Dili | Kullanıcının dili | Prompt'ta `Respond in {language}` |
| Cache | OpenAI Prompt Caching | %90 input indirimi (otomatik) |

---

## Maliyet Analizi

### Geliştirme Maliyeti

| Bileşen | Süre (Adam/Gün) |
|---------|-----------------|
| AI Service Altyapısı (API entegrasyonu, retry, error handling) | 1 |
| Prompt Template Sistemi (rapor tiplerine göre dinamik) | 1.5 |
| Kullanım Quota Sistemi (kullanıcı/firma bazlı limit) | 1.5 |
| UI - Rapor Sayfaları ("AI ile Yorumla" butonu) | 1 |
| Admin Panel (limit ayarları, kullanım istatistikleri) | 1 |
| Localization (4 dil) | 0.5 |
| Test & Prompt Tuning | 1.5 |
| Faturalama sistemi (token log, maliyet hesaplama) | 2 |
| **TOPLAM** | **10 gün** |

### İşletme Maliyeti (Aylık)

| Kullanım | Tahmini Maliyet |
|----------|-----------------|
| ~1000 istek/ay | $50-150 (~3.000-4.500 ₺) |

### GPT-5.2 Fiyatlandırma

| Tip | Fiyat |
|-----|-------|
| Input | $1.75 / 1M token |
| Output | $14 / 1M token |
| Cached Input | %90 indirim |

---

## Kullanıcı Limitleri (Öneri)

| Rol | Günlük | Aylık |
|-----|--------|-------|
| CustomerOperator | 5 | 50 |
| CustomerSupervisor | 10 | 100 |
| CustomerManager | 20 | 200 |
| Admin/QualitySpecialist | Limitsiz | Limitsiz |

---

## Teknik Mimari

### Akış

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Rapor Data    │────▶│   AI Service    │────▶│  Yorum/Analiz   │
│   (JSON)        │     │   (GPT-5.2)     │     │  (Markdown)     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌─────────────────┐
                        │  Token Log      │
                        │  (Faturalama)   │
                        └─────────────────┘
```

### Prompt Yapısı (Cache Optimizasyonu)

```
┌──────────────────────────────────────┐
│ 1. System Prompt (İngilizce, sabit)  │  ← Cache'lenir (%90 indirim)
│ 2. Rapor tipi talimatları            │
│ 3. Format kuralları                  │
├──────────────────────────────────────┤
│ 4. User Data (JSON, değişken)        │  ← Her istekte farklı
│ 5. Language instruction              │
└──────────────────────────────────────┘
```

### Entity'ler

```csharp
// Kullanım logu (faturalama için)
public class AIUsageLog : BaseEntity
{
    public int? UserId { get; set; }
    public int? CustomerPersonnelId { get; set; }
    public int? CustomerId { get; set; }
    public string ReportType { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal CostUSD { get; set; }
    public DateTime UsedAt { get; set; }
}

// Kullanıcı/firma quota'sı
public class AIUsageQuota : BaseEntity
{
    public int? UserId { get; set; }
    public int? CustomerPersonnelId { get; set; }
    public int? CustomerId { get; set; }
    public int DailyLimit { get; set; }
    public int MonthlyLimit { get; set; }
    public int DailyUsed { get; set; }
    public int MonthlyUsed { get; set; }
    public DateTime DailyResetAt { get; set; }
    public DateTime MonthlyResetAt { get; set; }
}
```

### Service Interface

```csharp
public interface IAIInsightService
{
    Task<AIInsightResult> GenerateInsightAsync(string reportType, object data, string language);
    Task<bool> CheckQuotaAsync(int? userId, int? customerPersonnelId);
    Task<AIUsageStats> GetUsageStatsAsync(int? customerId, DateTime from, DateTime to);
}
```

---

## Prompt Hazırlığı İçin Gerekenler

Promptları hazırlamak için şu bilgiler lazım:

1. **Rapor Listesi** - Hangi raporlar AI ile yorumlanacak?
2. **Data Yapıları** - Her rapor için örnek JSON
3. **Yorum Tonu** - Resmi / Samimi / Teknik
4. **Yorum Uzunluğu** - Kısa (2-3 cümle) / Orta (1 paragraf) / Detaylı

---

## Hedef Raporlar (Taslak)

| Rapor | Açıklama | Öncelik |
|-------|----------|---------|
| Performans Takibi | Değerlendirici performans analizi | Yüksek |
| Müşteri Kota Durumu | Firma bazlı hedef/gerçekleşme | Yüksek |
| Personel Karnesi | Çalışan bazlı değerlendirme özeti | Yüksek |
| Cezalı KL Raporu | Kritik hata analizi | Orta |
| Öneriler Raporu | İyileştirme önerileri özeti | Orta |
| Genel Değerlendirme Özeti | Dönemsel genel bakış | Orta |

---

## Faz Planı

### Faz 1: Temel Sistem (10 gün)
- AI Service altyapısı
- Statik prompt template'leri
- Kullanım quota sistemi
- UI entegrasyonu
- Admin panel
- Faturalama

### Faz 2: Gelişmiş Özellikler (Opsiyonel, +5.5 gün)
- Feedback sistemi (👍👎)
- Prompt versiyonlama
- A/B testing
- Auto-tuning
- Analytics dashboard

---

## Referanslar

- [OpenAI GPT-5.2 Duyurusu](https://openai.com/index/introducing-gpt-5-2/)
- [OpenAI API Modelleri](https://platform.openai.com/docs/models/)
- [Prompt Caching Dokümantasyonu](https://platform.openai.com/docs/guides/prompt-caching)

---

## Notlar

- OpenAI API her response'da `usage` objesi döndürüyor (prompt_tokens, completion_tokens, total_tokens)
- Bu bilgiyle kullanıcıya fatura kesilebilir
- Prompt caching otomatik çalışıyor, kod değişikliği gerektirmiyor
- System prompt'u başa, değişken data'yı sona koymak cache verimliliğini artırır
