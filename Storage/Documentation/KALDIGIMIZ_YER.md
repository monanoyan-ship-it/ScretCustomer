# Kaldığımız Yer - 15 Ocak 2026 (Son Güncelleme)

---

## ✅ TAMAMLANAN İŞLER (15 Ocak 2026 - Devam)

### CustomerPortal Raporları Eklendi

Müşteri portalına yeni rapor sayfaları eklendi. Bu raporlar admin versiyonlarının müşteri için uyarlanmış halidir.

**Değerlendirici bilgisi gizlendi:**
- `EvaluatorName` alanları müşteriye gösterilmiyor
- Excel export'larda da değerlendirici kolonu çıkarıldı

**Eklenen sayfalar:**
| Rapor | Sayfa | JS | API Endpoint |
|-------|-------|-----|--------------|
| Cezalı KL Raporu | `/CustomerPortal/Penalties` | `penalties.js` | `/api/customer/portal/reports/penalties` |
| Öneriler Raporu | `/CustomerPortal/Suggestions` | `suggestions.js` | `/api/customer/portal/reports/suggestions` |
| Temsilci Karnesi | `/CustomerPortal/PersonnelReportCard` | `personnelReportCard.js` | `/api/customer/portal/reports/personnel-report-card/{id}` |

**Yapılan değişiklikler:**
- `CustomerPortalApiController.cs`: Yeni endpoint'ler eklendi (Penalties, Suggestions, PersonnelReportCard)
- `CustomerPortalController.cs`: MVC action'lar eklendi
- `IReportService.cs`: `excludeEvaluator` parametresi eklendi
- `ReportService.cs`: CustomerId filtreleme ve excludeEvaluator desteği eklendi
- `_CustomerLayout.cshtml`: Sidebar'a yeni rapor linkleri eklendi (CustomerManager rolüne özel)
- DTO'lara CustomerId eklendi: `SuggestionsFilterDto`, `PersonnelReportCardFilterDto`

**Yetkilendirme:**
- Sadece `CustomerManager` rolü bu raporlara erişebilir
- Filtreleme otomatik: Müşteri sadece kendi firmasının verilerini görüyor

---

## ✅ TAMAMLANAN İŞLER (15 Ocak 2026)

### SubCriteria Seçim Tipi ve SurveyResults
- SubCriteria için SelectionType eklendi (Single/Multiple seçim)
- Question'a `ShowScoreInput` alanı eklendi (online anketlerde false)
- SurveyResults rapor sayfası oluşturuldu
- Proje tipi bazlı raporlama pattern'i oluşturuldu

### Proje Tipi Bazlı Raporlama Pattern'i
- Her rapor kendi proje tipini pozitif filtreleme ile seçmeli: `== ProjectTypes.Ids.CallAuditing`
- Yanlış: `!= ProjectTypes.Ids.OnlineSurvey` (negatif filtreleme)
- Doğru: `== ProjectTypes.Ids.CallAuditing` (pozitif filtreleme)
- DEVELOPMENT_PATTERNS.md'ye "14. Proje Tipi Bazlı Raporlama Pattern'i" bölümü eklendi

### scorePercentage Gösterimi Standardizasyonu
- Tüm `scorePercentage` değerleri artık `.toFixed(2)` ile gösteriliyor
- Önceki: `.toFixed(0)` veya `.toFixed(1)` (yuvarlama yapıyordu)
- Şimdi: `.toFixed(2)` (2 ondalık basamak, yuvarlama yok)

**Güncellenen dosyalar:**
- Views/Evaluations/Index.cshtml
- Views/CustomerPortal/Evaluations.cshtml
- Views/CustomerPortal/InternalEvaluations.cshtml
- Views/CustomerPortal/ExternalEvaluations.cshtml
- Views/FieldWorker/Index.cshtml
- Views/FieldWorker/Visits.cshtml
- Views/Listenings/Index.cshtml
- Views/Reports/Index.cshtml
- Views/Reports/PersonnelReportCard.cshtml

---

## 📝 YAPILACAKLAR (Backlog)

- [x] ~~Her rapora müşteri raporları kısmını da ekle~~ (TAMAMLANDI - 15 Ocak 2026)
  - Cezalı KL, Öneriler, Temsilci Karnesi raporları CustomerPortal'a eklendi

---

## ✅ TAMAMLANAN İŞLER (9 Ocak 2026)

### Excel → CSV Dönüşümleri

**Personel Import:**
| Excel Dosyası | CSV Çıktısı | İçerik |
|---------------|-------------|--------|
| `Bosch Home Come.xlsx` | `Bosch_Home_Come_personnel.csv` | 4 Manager, 38 Operator (42 kişi) |

**Checklist Import:**
| Excel Dosyası | CSV Çıktısı | İçerik |
|---------------|-------------|--------|
| `Bosch cozumleme.xlsx` | `Bosch_checklist_import.csv` | 23 soru (SubCriteria dahil) |
| `Boyner ınbound.xlsx` | `Boyner_inbound_checklist.csv` | 49 soru (26 Scored, 11 YellowCard, 11 RedCard, 1 Unscored) |

### Checklist Import Güncellemesi
- Import artık **yeni checklist oluşturuyor** (mevcut güncelleme yerine)
- Parametreler: `checklistName`, `customerId` (opsiyonel), `description` (opsiyonel)
- API: `POST /api/import/checklist?checklistName=...&customerId=...&description=...`

### Checklist Soru Layout Düzenlemesi
- Soru kartı layout değişti:
  - Satır 1: Soru Grubu (tam genişlik)
  - Satır 2: Soru Metni (tam genişlik)
  - Satır 3: Puanlama - Ağırlık Puanı - Maks Puan - Ceza Tipi - Sıra - Zorunlu
- `Question.GroupName` MaxLength(200) limiti kaldırıldı (artık sınırsız)
- Migration: `RemoveGroupNameMaxLength`

### Checklist CSV Import Özelliği
- `/Import/Index` sayfasına yeni "Checklist İçe Aktarma" tabı eklendi
- API: `POST /api/import/checklist?checklistId={id}&clearExisting=false`
- Şablon: `GET /api/import/checklist/template`

**CSV Formatı:**
```csv
GroupName,QuestionText,WeightPoints,MaxPoints,ScoringType,PenaltyType,SubCriteria,Order,IsRequired,HelpText
İletişim,Müşterinin sözü kesildi,3,3,Scored,None,"Alt kriter 1|Alt kriter 2",1,false,
Kritik,Kabul Edilemez Eylemler,100,1,Penalty,RedCard,"KVKK ihlali|Polemik",2,true,Kırmızı kart
```

**Kolonlar:**
| Kolon | Açıklama | Zorunlu | Varsayılan |
|-------|----------|---------|------------|
| GroupName | Soru grubu adı | Hayır | - |
| QuestionText | Soru metni | EVET | - |
| WeightPoints | Ağırlık puanı | Hayır | 1 |
| MaxPoints | Maks puan (1-10) | Hayır | 5 |
| ScoringType | Scored/Unscored/Penalty | Hayır | Scored |
| PenaltyType | None/YellowCard/RedCard | Hayır | None |
| SubCriteria | Alt kriterler (pipe ile ayrılmış) | Hayır | - |
| Order | Sıra numarası | Hayır | Otomatik |
| IsRequired | Zorunlu mu | Hayır | false |
| HelpText | Yardımcı metin | Hayır | - |

### Form.cshtml Pattern İhlali Düzeltildi
- `Views/Evaluations/Form.cshtml` silindi (ayrı sayfa YASAK)
- `wwwroot/js/Evaluations/Form.js` silindi
- `EvaluationsController.Form()` action kaldırıldı
- Pattern'e göre: Değerlendirmeler sadece Index.cshtml'deki modal ile yapılır

---

## 📋 IMPORT ŞABLONLARI (Boş Kalıp)

### Personel Import CSV
```csv
FullName,Username,Email,Password,Role,RoleId,Company,Organization
Ahmet Yılmaz,ahmet.yilmaz,a@b.com,user@123,CustomerOperator,3,Firma Adı,Merkez
```
- **Endpoint:** `POST /api/import/personnel`
- **Şablon:** `GET /api/import/personnel/template`

### Checklist Import CSV
```csv
GroupName,QuestionText,WeightPoints,MaxPoints,ScoringType,PenaltyType,SubCriteria
Grup Adı,Soru metni,3,5,Scored,None,"Alt kriter 1|Alt kriter 2"
```
- **Endpoint:** `POST /api/import/checklist?checklistId={id}`
- **Şablon:** `GET /api/import/checklist/template`

---

## ✅ TAMAMLANAN İŞLER (1 Ocak 2026)

### Faz 1: Değerlendirme Akışı Güncellemesi

**Cascade Dropdown (Önce Organizasyon, Sonra Personel):**
- `EvaluationFormDto`'ya `AvailableOrganizations` eklendi
- Yeni endpoint: `GET /api/evaluations/personnel-by-org/{organizationId}`
- `SubmitEvaluationDto`'ya `EvaluatedOrganizationId` eklendi
- Frontend: Organizasyon seçimi ZORUNLU yapıldı
- Organizasyon seçilmeden personel listesi gösterilmiyor

### Faz 2: Dashboard Metrikleri

**Yeni Entity: SystemSetting**
- Sistem ayarları için key-value tablosu
- `DailyEvaluationTarget = 55` varsayılan değer
- SystemSettingService ve API controller oluşturuldu

**Yeni Dashboard Widget'ları:**
- Günlük dinleme metrikleri (Bugün, Bu Hafta, Bu Ay)
- Günlük hedef progress bar
- Son 7 günün trend grafiği
- En çok dinleyen kullanıcılar (bugün ve bu ay)
- Dönem hedef takibi

**Yeni API Endpoint'leri:**
- `GET /api/dashboard/daily-metrics`
- `GET /api/dashboard/user-performance`
- `GET /api/dashboard/target-progress`

### Faz 3: Hiyerarşi Görünümü (Tree View)

**Tree View Özellikleri:**
- Organizasyonlar ağaç yapısında gösteriliyor
- Expand/collapse toggle
- Drag & drop ile parent değiştirme
- Alt organizasyon ekleme butonu
- Düzenle ve sil butonları her node'da

**Backend:**
- `MoveOrganizationAsync` metodu eklendi (ICustomerOrganizationService)
- `PUT /api/customer-organizations/{id}/move` endpoint'i eklendi
- Döngüsel referans kontrolü mevcut

**Frontend:**
- KnockoutJS template kullanımı (recursive tree)
- Native HTML5 drag & drop API
- CSS stilleri eklendi

---

## ✅ TAMAMLANAN İŞLER (31 Aralık 2025 - Önceki)

### Modül Temizliği ve FieldWorker Kaldırma

**Kaldırılan Modüller:**
- FieldWorker entity ve ilgili tüm dosyalar (Controller, Service, Repository, DTO, View, JS)
- Organization modülü (OrganizationUnit, Delegation)
- Visit modülü (VisitDetails, CustomerVisit, VisitSector, VisitFieldDefinition)
- Calls modülü

**Assignment Yapısı Güncellendi:**
- Assignment artık doğrudan User'a bağlı (`AssignedUserId`)
- `AssignedFieldWorkerId` kaldırıldı
- `GetByFieldWorkerIdAsync` metodu `GetByUserIdAsync` olarak değiştirildi

**Sidebar Sadeleştirildi:**
- "İşlemler" menüsü kaldırıldı
- "Görevlerim" ve "Değerlendirmeler" üst seviye menü öğeleri olarak taşındı
- "Saha Çalışanları" menü öğesi kaldırıldı

**Yeni Eklenenler:**
- CustomerOrganizations controller/view eklendi (Müşteri Organizasyonları yönetimi)

**Migration:**
- `RemoveVisitAndCallModules`
- `RemoveOrganizationUnitAndDelegation`
- `RemoveFieldWorkerEntity`

**Commit:** `845b0b7` - FieldWorker entity kaldırıldı, modül temizliği yapıldı

---

## ✅ TAMAMLANAN İŞLER (8 Ocak 2026)

### Section Yapısı Temizliği

**Section Kullanımı Kaldırıldı:**
- `EvaluationService.cs`: `Sections.SelectMany` → `Questions.Where`
- `EvaluationService.cs`: `ThenInclude(c => c.Sections)` → `ThenInclude(c => c.Questions)`
- `AssignmentRepository.cs`: Aynı düzeltme
- `ProcessEvaluationAsync`: Questions direkt Checklist'e bağlı

**PenaltyType'a Göre Gruplama:**
- Sorular (penaltyType = None) → Mavi başlık
- Sarı Kartlar (penaltyType = YellowCard) → Sarı başlık
- Kırmızı Kartlar (penaltyType = RedCard) → Kırmızı başlık

**UI Düzeltmeleri:**
- Her soruda ağırlık/max badge gösterimi
- Puan barındaki ağırlık grupları kaldırıldı (grup başlıklarına taşındı)
- Dönem uyarısı kaldırıldı
- Detay modalı: `with` binding düzeltmesi (boş modal sorunu)

**Commit:** `6873a9e` - Section yapısı kaldırıldı, PenaltyType gruplama eklendi

---

## 📝 NOTLAR

### Entity Durumu
- **Section entity hala projede VAR** ama kullanılmıyor
- Questions artık direkt Checklist'e bağlı (`SectionId` nullable)
- İleride Section entity tamamen silinebilir

### GroupName Hakkında
- Question entity'sinde `GroupName` alanı var
- Bu alan sadece **RAPORLAMA** için kullanılacak
- UI'da gruplama yapılmıyor, sadece PenaltyType'a göre sıralama var

---

## YENİ İŞ MODELİ

### 1. ROLLER

#### User (Bizim Şirket - Neyce Akademi) - 3 Rol
| Rol | Yetkiler |
|-----|----------|
| **Admin** | Sistem yöneticisi, her şeye erişim |
| **Kalite Uzmanı** | Değerlendirme yapar, dönem açar, personel yükler |
| **Saha Çalışanı** | Sahada checklist puanlar (Kalite Uzmanı gibi) |

#### CustomerPersonnel (Firma Tarafı) - 3 Rol
| Rol | Yetkiler |
|-----|----------|
| **Müşteri Yöneticisi** | Tüm raporları görür (Firma Yetkilisi) |
| **Müşteri Süpervizörü** | Kendi takımının raporlarını görür (Takım Lideri) |
| **Müşteri Operatörü** | Sadece kendi değerlerini görür (Değerlendirilecek personel) |

> **NOT:** Görüntüleyici rolü KALDIRILDI. Rol = Ünvan (ayrı ayrı değil)

---

### 2. HİYERARŞİ YAPISI

```
Firma (Customer)
  └── Organizasyon (CustomerOrganization)
        └── Personel (CustomerPersonnel)
              └── Bağlı Personel (SupervisorId ile)

Örnek:
Firma: Ford
  └── Organizasyon: Concentrix
        └── Personel: Takım Lideri (Süpervizör)
              └── Bağlı Personel: Müşteri Temsilcisi (Operatör)
```

**Önemli İlişkiler:**
- Bir personel birden çok organizasyona yöneticilik yapabilir (many-to-many)
- Bir değerlendirici birden çok organizasyonu değerlendirebilir (many-to-many)

---

### 3. DEĞERLENDİRME AKIŞI

```
1. Organizasyon/Departman seç
         ↓
2. O departmandaki personeli seç
         ↓
3. Çağrı bilgisi gir (tarih, saat, dakika)
         ↓
4. Checklist'i doldur/puanla
         ↓
5. "Formu Kapat" → Rapora yansır (geri alınamaz)
         ↓
6. Hata varsa → ID ver → Yönetici taslağa alır → Tekrar değerlendir
```

---

## Teknik Notlar

### Database
- Database tazelenebilir (yeni migration ile)
- Tüm eski veriler silinecek

### Build Durumu
- Son build: 0 Error, 0 Warning

---

## Video Transkript Dosyaları

- `Storage/ScreenShots/video1_transcript.txt`
- `Storage/ScreenShots/video2_transcript.txt`
