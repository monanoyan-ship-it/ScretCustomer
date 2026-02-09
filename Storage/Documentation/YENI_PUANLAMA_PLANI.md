# Yeni Puanlama Sistemi Planı

> **Tarih:** 2026-01-25
> **Kaynak:** SESTEK Kalite Değerlendirme Sistemi videosu
> **Video:** `Storage/ScreenShots/yenipuansistemi/bandicam 2026-01-23 13-12-34-806.mp4`

---

## 1. VİDEO ANALİZİ ÖZETİ

SESTEK sisteminde gördüğümüz puanlama yapısı:

| Seçenek | Puan Örneği | Açıklama |
|---------|-------------|----------|
| **Evet** | +5, +10, +15 | Kriter karşılandı - tam puan |
| **Geliştirilmeli** | +2.5, +5, +7.5 | Kısmen karşılandı - yarım puan |
| **Hayır** | 0, -5, -10 | Karşılanmadı - sıfır veya eksi puan |
| **Gerekmedi** | N/A | Bu görüşmede geçerli değil |

**Örnek Sorular (Excel'den):**
- "Etkin dinleme yapıldı mı?" → Evet: +15, Geliştirilmeli: +7.5, Hayır: 0
- "Müşteri temsile yetkin miydi?" (Ödül & Ceza) → Evet: +10, Geliştirilmeli: 0, Hayır: -10
- "Doğru bilgi verildi mi?" → Evet: +7.5, Geliştirilmeli: 0, Hayır: -7.5

---

## 2. MEVCUT SİSTEMİMİZ

### 2.1 Puanlama Yöntemleri (ScoringMethods)

| ID | SystemName | Açıklama | Kullanım |
|----|------------|----------|----------|
| 1 | Maximum | Maksimum puan üzerinden | **KULLANILIYOR** - Mevcut tüm checklistler |
| 2 | Average | Ortalama | **KULLANILMIYOR** - SİLİNECEK |
| 3 | WeightedAverage | Ağırlıklı ortalama | **KULLANILMIYOR** - SİLİNECEK |
| 4 | Sum | Toplam | **YENİ SİSTEM İÇİN** - "Kriter Toplam" olacak |

### 2.2 Checklist Tipleri (ChecklistTypes)

| ID | SystemName | Açıklama |
|----|------------|----------|
| 1 | CallPerformance | Çağrı Performans değerlendirmesi |
| 2 | PhysicalAudit | Fiziksel denetim |
| 3 | MysteryShopping | Gizli Müşteri |
| 4 | OnlineEvaluation | Online değerlendirme |
| 5 | Survey | Genel anket |

### 2.3 Mevcut Hesaplama Mantığı (Maximum)

```
Soru: MaxPoints=5, WeightPoints=10
Kullanıcı girişi: GivenPoints=3

Hesaplama: (GivenPoints / MaxPoints) * WeightPoints
Sonuç: (3 / 5) * 10 = 6 puan
```

---

## 3. YENİ SİSTEM TASARIMI

### 3.1 Temel Fikir

**ScoringMethodId** alanını kullanarak iki farklı hesaplama modu:

| ScoringMethodId | Yeni Adı | Hesaplama |
|-----------------|----------|-----------|
| 1 (Maximum) | Maksimum | `(GivenPoints / MaxPoints) * WeightPoints` |
| 4 (Sum) | **Kriter Toplam** | `Seçilen seçeneğin Points değeri direkt alınır` |

### 3.2 "Kriter Toplam" Modu Nasıl Çalışacak?

```
Soru: "Etkin dinleme yapıldı mı?"
Seçenekler (QuestionSubCriteria olarak):
  - "Evet" → WeightPoints: +15
  - "Geliştirilmeli" → WeightPoints: +7.5
  - "Hayır" → WeightPoints: 0

Kullanıcı "Geliştirilmeli" seçerse → Direkt +7.5 puan alır
```

### 3.3 Yapılacak Değişiklikler

#### A. TypeDefinitions.cs Değişiklikleri

```csharp
public static class ScoringMethods
{
    // Maximum kalıyor (mevcut sistem)
    public static readonly TypeItem Maximum = new(1, "Maximum", "ScoringMethod.Maximum",
        "Maksimum puan üzerinden hesaplama", "bi-arrow-up-circle", "bg-primary", 1, isDefault: true);

    // Average SİLİNİYOR
    // WeightedAverage SİLİNİYOR

    // Sum → "Kriter Toplam" olarak yeniden adlandırılıyor
    public static readonly TypeItem CriteriaTotal = new(4, "CriteriaTotal", "ScoringMethod.CriteriaTotal",
        "Kriter Toplam - Seçenek puanları toplanır", "bi-plus-circle", "bg-success", 2);

    public static IEnumerable<TypeItem> All => new[] { Maximum, CriteriaTotal };

    public static class Ids
    {
        public const int Maximum = 1;
        public const int CriteriaTotal = 4; // Eski Sum ID'si korunuyor (DB uyumluluğu)
    }
}
```

#### B. Çeviri Dosyaları (resources)

```json
{
    "ScoringMethod.Maximum": {
        "tr": "Maksimum",
        "en": "Maximum"
    },
    "ScoringMethod.CriteriaTotal": {
        "tr": "Kriter Toplam",
        "en": "Criteria Total"
    }
}
```

#### C. QuestionSubCriteria Entity (Mevcut - Değişiklik YOK)

```csharp
public class QuestionSubCriteria : BaseEntity
{
    public int QuestionId { get; set; }
    public string Description { get; set; }  // "Evet", "Geliştirilmeli", "Hayır"
    public decimal WeightPoints { get; set; } // +15, +7.5, 0, -10
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
```

**NOT:** Mevcut yapı zaten uygun! SubCriteria'yı "seçenek" olarak kullanacağız.

#### D. EvaluationService.CalculateScoreCore Değişikliği

```csharp
private decimal CalculateQuestionScore(Question question, Answer answer, int checklistScoringMethodId)
{
    // Kriter Toplam modu
    if (checklistScoringMethodId == ScoringMethods.Ids.CriteriaTotal)
    {
        // Seçilen seçeneğin (SubCriteria) WeightPoints değerini al
        var selectedOption = answer.SubCriteriaSelections?.FirstOrDefault();
        if (selectedOption?.SubCriteria != null)
        {
            return selectedOption.SubCriteria.WeightPoints;
        }
        return 0;
    }

    // Maksimum modu (mevcut sistem)
    if (question.MaxPoints > 0 && answer.GivenPoints.HasValue)
    {
        return (answer.GivenPoints.Value / question.MaxPoints) * question.WeightPoints;
    }

    return 0;
}
```

#### E. UI Değişiklikleri (Checklist Editor)

"Kriter Toplam" seçildiğinde:
- `MaxPoints` input'u gizlenir (gerek yok)
- `WeightPoints` input'u gizlenir (gerek yok)
- SubCriteria'lar "Seçenekler" olarak gösterilir
- Her seçeneğe puan değeri girilir

---

## 4. VERİTABANI DEĞİŞİKLİKLERİ

### 4.1 Migration: Kullanılmayan ScoringMethod'ları Temizle

```csharp
// Önce kontrol et: Average veya WeightedAverage kullanan checklist var mı?
var checklistsWithOldMethods = await _context.Checklists
    .Where(c => c.ScoringMethodId == 2 || c.ScoringMethodId == 3)
    .ToListAsync();

// Varsa Maximum'a çevir
foreach (var checklist in checklistsWithOldMethods)
{
    checklist.ScoringMethodId = ScoringMethods.Ids.Maximum;
}
```

### 4.2 Çeviri Kayıtlarını Güncelle

```sql
-- Eski çevirileri sil
DELETE FROM LocaleStringResources WHERE ResourceKey LIKE 'ScoringMethod.Average%';
DELETE FROM LocaleStringResources WHERE ResourceKey LIKE 'ScoringMethod.WeightedAverage%';
DELETE FROM LocaleStringResources WHERE ResourceKey LIKE 'ScoringMethod.Sum%';

-- Yeni çeviri ekle
INSERT INTO LocaleStringResources (LanguageId, ResourceKey, ResourceValue)
VALUES
(1, 'ScoringMethod.CriteriaTotal', 'Kriter Toplam'),
(2, 'ScoringMethod.CriteriaTotal', 'Criteria Total');
```

---

## 5. ÖRNEK SENARYO

### Mevcut Sistem (Maximum - ScoringMethodId=1)

```
Checklist: "Çağrı Kalite Formu" (Maximum)

Soru 1: "Selamlama yapıldı mı?"
  - MaxPoints: 1 (Evet/Hayır)
  - WeightPoints: 10
  - Kullanıcı "Evet" (1) seçerse → (1/1)*10 = 10 puan
  - Kullanıcı "Hayır" (0) seçerse → (0/1)*10 = 0 puan

Soru 2: "İletişim kalitesi?"
  - MaxPoints: 5 (1-5 arası)
  - WeightPoints: 20
  - Kullanıcı 4 verirse → (4/5)*20 = 16 puan
```

### Yeni Sistem (Kriter Toplam - ScoringMethodId=4)

```
Checklist: "SESTEK Kalite Formu" (Kriter Toplam)

Soru 1: "Etkin dinleme yapıldı mı?"
  SubCriteria (Seçenekler):
    - "Evet" → WeightPoints: +15
    - "Geliştirilmeli" → WeightPoints: +7.5
    - "Hayır" → WeightPoints: 0
  - Kullanıcı "Geliştirilmeli" seçerse → Direkt +7.5 puan

Soru 2: "Müşteri temsile yetkin miydi?" (Ödül & Ceza)
  SubCriteria (Seçenekler):
    - "Evet" → WeightPoints: +10
    - "Geliştirilmeli" → WeightPoints: 0
    - "Hayır" → WeightPoints: -10
  - Kullanıcı "Hayır" seçerse → Direkt -10 puan

Toplam: 7.5 + (-10) = -2.5 puan
```

---

## 6. CHECKLIST TİPİ İLİŞKİSİ

| ChecklistType | Önerilen ScoringMethod |
|---------------|------------------------|
| CallPerformance | Maximum VEYA Kriter Toplam (müşteriye bağlı) |
| PhysicalAudit | Maximum |
| MysteryShopping | Maximum |
| OnlineEvaluation | Maximum VEYA Kriter Toplam |
| Survey | Maximum (puansız da olabilir) |

**NOT:** ChecklistType ile ScoringMethod bağımsız. Aynı tipte checklist farklı puanlama yöntemi kullanabilir.

---

## 7. UYGULAMA ADIMLARI

### Adım 1: TypeDefinitions Güncelle
- [ ] Average ve WeightedAverage'ı sil
- [ ] Sum'ı CriteriaTotal olarak yeniden adlandır
- [ ] Çevirileri güncelle

### Adım 2: EvaluationService Güncelle
- [ ] CalculateScoreCore metoduna CriteriaTotal modu ekle
- [ ] Negatif puan desteği ekle (zaten var mı kontrol et)

### Adım 3: Checklist Editor UI Güncelle
- [ ] ScoringMethod dropdown'ında sadece Maximum ve Kriter Toplam göster
- [ ] Kriter Toplam seçildiğinde soru editörünü değiştir:
  - MaxPoints/WeightPoints gizle
  - SubCriteria'ları "Seçenekler" olarak göster
  - Her seçeneğe puan girişi

### Adım 4: Değerlendirme UI Güncelle
- [ ] Kriter Toplam modunda:
  - Puan girişi (1-5) yerine seçenek seçimi göster
  - Seçenekleri radio button olarak göster

### Adım 5: Test
- [ ] Mevcut checklistler çalışıyor mu? (Maximum)
- [ ] Yeni Kriter Toplam checklistler doğru hesaplıyor mu?
- [ ] Negatif puanlar doğru işleniyor mu?
- [ ] Toplam puan negatif olabilir mi? (Evet, olabilir)

---

## 8. RİSKLER VE DİKKAT EDİLECEKLER

1. **Mevcut Veriler:** Average/WeightedAverage kullanan checklist varsa önce Maximum'a çevir
2. **Negatif Toplam:** Kriter Toplam modunda toplam puan negatif olabilir - UI bunu göstermeli
3. **SubCriteria Çift Kullanım:** Hem "öneri/eksiklik" hem "seçenek" olarak kullanılıyor - UI'da net ayrım yapılmalı
4. **Raporlama:** Negatif puanları raporlarda nasıl göstereceğiz?

---

## 9. SONUÇ

**Minimum değişiklikle yeni puanlama sistemi:**
- Yeni entity YOK
- Yeni migration SADECE temizlik için
- Mevcut SubCriteria yapısı kullanılıyor
- ScoringMethodId ile mod seçimi

**Avantajlar:**
- Geriye uyumlu
- Esnek (her checklist kendi modunu seçebilir)
- Az kod değişikliği

---

## 10. UI MİMARİSİ KARARLARI

### 10.1 Checklist Editor - Mod Seçimi Önce

**Akış:**
1. "Yeni Checklist" butonuna tıklanır
2. **Mod Seçim Modalı** açılır:
   - "Maksimum" - Klasik puanlama (0-MaxPoints arası giriş)
   - "Kriter Toplam" - SESTEK tarzı (seçenek bazlı)
3. Mod seçildikten sonra **ilgili Popup** açılır

**Dosya Yapısı:**
```
Views/Checklists/
├── Index.cshtml          # Liste + Mod seçim modalı
├── PopupMaximum.cshtml   # Maksimum mod için editör popup
└── PopupCriteriaTotal.cshtml  # Kriter Toplam mod için editör popup

wwwroot/js/Checklists/
├── index.js              # Liste VM + mod seçimi
├── popup-maximum.js      # Maksimum editör VM
└── popup-criteria-total.js  # Kriter Toplam editör VM
```

**Avantajları:**
- Kodlar ayrı ve temiz
- Her mod kendi popup'ında bağımsız
- Karmaşık if/else UI mantığı yok
- Bakımı kolay

### 10.2 Güncelleme (Edit) Akışı

Listede checklist'e tıklandığında:
1. Checklist'in `ScoringMethodId`'si kontrol edilir
2. İlgili popup açılır:
   - Maximum → PopupMaximum.cshtml
   - CriteriaTotal → PopupCriteriaTotal.cshtml

### 10.3 Değerlendirme (Evaluation) - Popup Kullanımı

**Mevcut Durum:** Modal kullanılıyor (karmaşık, çok büyük)

**Yeni Yaklaşım:** Popup kullanılacak

**Akış:**
1. Atama listesinde "Değerlendir" butonuna tıklanır
2. Checklist'in `ScoringMethodId`'sine göre ilgili popup açılır:
   - Maximum → EvaluationPopupMaximum.cshtml
   - CriteriaTotal → EvaluationPopupCriteriaTotal.cshtml

**Dosya Yapısı:**
```
Views/Evaluations/
├── Index.cshtml                    # Atama listesi
├── PopupMaximum.cshtml             # Maksimum mod değerlendirme
└── PopupCriteriaTotal.cshtml       # Kriter Toplam mod değerlendirme

wwwroot/js/Evaluations/
├── Index.js                        # Liste VM
├── popup-maximum.js                # Maksimum değerlendirme VM
└── popup-criteria-total.js         # Kriter Toplam değerlendirme VM
```

### 10.4 Popup Avantajları (Modal'a Göre)

| Özellik | Modal | Popup |
|---------|-------|-------|
| Boyut | Sınırlı (modal-lg max) | Tam ekran olabilir |
| Ana sayfa görünürlüğü | Kapalı (backdrop) | Açık (yan yana) |
| Çoklu açılabilirlik | Hayır | Evet |
| Büyük formlar | Scroll problemi | Rahat |
| Kod ayrımı | Aynı dosyada | Ayrı dosyalarda |
| WYSIWYG/Editor | Zor | Kolay |

### 10.5 Popup Standart Yapısı (_LayoutPopup)

```html
@{
    Layout = "_LayoutPopup";
    ViewData["Title"] = "Popup Başlığı";
}

<div id="popup-app">
    <!-- Header -->
    <div class="popup-header">
        <h4><i class="bi bi-icon"></i> Başlık</h4>
        <div class="d-flex gap-2">
            <button class="btn btn-primary btn-sm" data-bind="click: save">Kaydet</button>
            <button class="btn btn-secondary btn-sm" onclick="window.close()">Kapat</button>
        </div>
    </div>

    <!-- Content -->
    <div class="card shadow-sm">
        <!-- Form içeriği -->
    </div>
</div>

<script>
    window.popupConfig = { itemId: @(ViewBag.ItemId ?? "null") };
</script>

@section Scripts {
    <script src="~/js/Module/popup.js"></script>
}
```

---

## 11. UYGULAMA SIRASI (GÜNCELLENMİŞ)

### Faz 1: Backend Temelleri
- [ ] TypeDefinitions: Average, WeightedAverage sil
- [ ] TypeDefinitions: Sum → CriteriaTotal yeniden adlandır
- [ ] Çevirileri güncelle
- [ ] EvaluationService.CalculateScoreCore'a CriteriaTotal modu ekle

### Faz 2: Checklist Editor (Popup Mimarisi)
- [ ] Index.cshtml'e mod seçim modalı ekle
- [ ] PopupMaximum.cshtml oluştur (mevcut editör taşınacak)
- [ ] PopupCriteriaTotal.cshtml oluştur (yeni)
- [ ] popup-maximum.js oluştur
- [ ] popup-criteria-total.js oluştur
- [ ] index.js'e mod seçim ve popup açma mantığı ekle

### Faz 3: Değerlendirme (Popup Mimarisi)
- [ ] Mevcut modal yapısını analiz et
- [ ] PopupMaximum.cshtml oluştur (mevcut modal taşınacak)
- [ ] PopupCriteriaTotal.cshtml oluştur (yeni)
- [ ] popup-maximum.js oluştur
- [ ] popup-criteria-total.js oluştur
- [ ] Index.js'e popup açma mantığı ekle

### Faz 4: Test ve Doğrulama
- [ ] Mevcut Maximum checklistler çalışıyor mu?
- [ ] Yeni CriteriaTotal checklistler oluşturulabiliyor mu?
- [ ] Değerlendirmeler doğru hesaplanıyor mu?
- [ ] Negatif puanlar doğru işleniyor mu?
- [ ] Raporlar doğru gösteriyor mu?
