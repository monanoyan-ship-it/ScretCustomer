# Kaldığımız Yer - 12 Ocak 2026 (Son Güncelleme)

---

## 📝 YAPILACAKLAR (Backlog)

- [ ] Her rapora müşteri raporları kısmını da ekle (Raporlar menüsündeki her rapor için müşteri portalında da görünecek versiyon)

---

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

## ✅ TAMAMLANAN İŞLER (31 Aralık 2025 Gece - Önceki)

### AssignmentPeriod (Dönem) Sistemi

**Yeni Entity ve API:**
- `AssignmentPeriod` entity oluşturuldu (Ad, Başlangıç/Bitiş tarihi, Hedef sayısı, Durum)
- `PeriodStatus` enum: Open, Closed
- Evaluation entity'ye `AssignmentPeriodId` eklendi
- Assignment entity'ye `Periods` navigation property eklendi

**API Endpoints:**
- `GET /api/assignments/{id}/periods` - Dönemleri listele
- `POST /api/assignments/{id}/periods` - Yeni dönem oluştur
- `PUT /api/assignments/{id}/periods/{periodId}` - Dönem güncelle
- `POST /api/assignments/{id}/periods/{periodId}/close` - Dönemi kapat
- `POST /api/assignments/{id}/periods/{periodId}/reopen` - Dönemi yeniden aç
- `DELETE /api/assignments/{id}/periods/{periodId}` - Dönem sil

**UI Değişiklikleri:**
- Assignment detay modalında dönem tablosu gösteriliyor
- "Dönem Ekle" butonu ve modal eklendi (otomatik ay adı ve tarih)
- Değerlendirme formuna dönem seçimi eklendi (`AvailablePeriods`, `SelectedPeriodId`)

**Personel Seçimi Geliştirmesi:**
- Değerlendirmede personel seçimi artık checklist'in `CustomerId` ve `CustomerOrganizationId` alanlarına göre filtreleniyor
- Süpervizör (CustomerSupervisor) rolündeki personeller hariç tutuluyor
- Sadece ilgili firma/organizasyonun operatörleri listeleniyor

**Migration:** `AssignmentPeriods` migration oluşturuldu

---

## ✅ TAMAMLANAN İŞLER (31 Aralık 2025 - Önceki)

### Checklist Yapısı Refaktör (31 Aralık 2025)

**BÜYÜK DEĞİŞİKLİK:** Section/Grup katmanı kaldırıldı!

#### Önceki Yapı (Eski)
```
Checklist → Sections → Questions → SubCriteria
```

#### Yeni Yapı (Şu An)
```
Checklist → Questions → SubCriteria
```

#### Yapılan Değişiklikler

**Entity Değişiklikleri:**
- Question entity'e `ChecklistId` eklendi (direkt bağlantı)
- Question entity'e `ScaleSteps` eklendi (1-4 kırılım sayısı)
- Checklist entity'e `LikertScale` eklendi (0-5 ölçeği)
- Question'dan `Type`, `Points`, `MaxPoints`, `OptionsJson` kaldırıldı
- `SectionId` nullable yapıldı (geriye uyumluluk için)

**Yeni Puanlama Sistemi:**
- `LikertScale` (Checklist seviyesi): 0-5 değerlendirme ölçeği
- `WeightPoints` (Soru seviyesi): Ağırlık puanı
- `ScaleSteps` (Soru seviyesi): 1-4 kırılım (Evet/Hayır için 1, detaylı için 4)
- `ScoringType`: Scored (Puanlı), Unscored (Puansız), Penalty (Cezalı)
- Formül: `(cevap / ScaleSteps) * WeightPoints`

**Migration:**
- `DirectQuestionChecklistStructure` migration oluşturuldu
- Mevcut sorular için ChecklistId otomatik dolduruldu (Section'dan alınarak)

**Güncellenen Dosyalar:**
- `Question.cs`, `Checklist.cs` - Entity güncellemeleri
- `ChecklistDto.cs`, `UpdateChecklistDto.cs` - DTO güncellemeleri
- `ChecklistRepository.cs` - Questions direkt include
- `ChecklistService.cs` - Section'sız mapping
- `EvaluationService.cs` - Yeni puanlama formülü
- `ReportService.cs` - WeightPoints kullanımı
- `QuestionConfiguration.cs` - Yeni ilişki yapılandırması
- `SeedData.cs` - Yeni alanlarla seed data
- `Views/Checklists/Index.cshtml` - Tamamıyla yeniden yazıldı
- `wwwroot/js/Checklists/checklist.js` - SectionModel kaldırıldı

**Build Durumu:** ✅ Başarılı

---

## ✅ TAMAMLANAN İŞLER (30 Aralık 2025)

### Backend Değişiklikleri
1. **UserRole enum** → 3 rol: `Admin`, `QualitySpecialist`, `FieldWorker`
2. **CustomerPersonnelRole enum** → 3 rol: `CustomerManager`, `CustomerSupervisor`, `CustomerOperator`
3. **Yeni Entity'ler oluşturuldu:**
   - `CustomerOrganization` - Firma altında organizasyon hiyerarşisi
   - `CustomerOrganizationManager` - Personel-Organizasyon yönetici ilişkisi
   - `CustomerPersonnelOrganizationAccess` - Değerlendirici erişim ilişkisi
4. **CustomerPersonnel'e eklenenler:** `SupervisorId`, `OrganizationId`, navigation properties
5. **Evaluation'a eklenenler:** `EvaluatedCustomerPersonnelId`, `EvaluatedOrganizationId`
6. **Migration:** `CustomerOrganizationHierarchy` oluşturuldu ve database'e uygulandı
7. **Test projesi:** `UserServiceTests.cs` güncellendi (Evaluator → QualitySpecialist)

### Frontend Değişiklikleri
- `customer-personnel.js` - CustomerViewer kaldırıldı
- `localization.js` - Tüm roller güncellendi
- `Users/Index.js` ve `Index.cshtml` - Rol seçenekleri güncellendi (3 rol)
- `Profile/Index.js` - Rol isimleri güncellendi
- `auth.service.js` - `isQualitySpecialist()`, `isFieldWorker()` eklendi
- `dashboard.js` - Yeni rol kontrolleri
- `_Sidebar.cshtml` - Menü erişim kontrolleri güncellendi (isTeamLeader → isQualitySpecialist)

### Düzeltilen Dosyalar (Eski Roller)
- `SeedData.cs` - TeamLeader/Evaluator → QualitySpecialist
- `PermissionsApiController.cs` - Rol isimleri güncellendi
- `CustomerPersonnelService.cs` - CustomerViewer kaldırıldı
- `UserServiceTests.cs` - Evaluator → QualitySpecialist

### Build Durumu
✅ **Build Başarılı** - 0 Error, 0 Warning (API + Test Projesi)

---

## SON DURUM - MÜŞTERİ GERİBİLDİRİM ANALİZİ

### Bugün Yapılanlar (30 Aralık 2025)

#### Video Transkript Analizi
- [x] 2 yeni video transkripti çıkarıldı (Whisper ile)
- [x] Müşteri eleştirileri analiz edildi
- [x] İş modeli yeniden tanımlandı
- [x] Entity tasarımı yapıldı
- [x] Tüm backend/frontend değişiklikleri uygulandı

#### Kritik Düzeltmeler
- **Ana iş modeli:** Gizli Müşterilik DEĞİL → **Çağrı Değerlendirmesi**
- **Atama mantığı:** Tek görev DEĞİL → Checklist atanır, kullanıcı istediği kadar değerlendirme yapar

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

### 4. DÖNEM AÇMA

```
Kalite Uzmanı → Yeni Dönem Ekle
  ├── Proje: Ford Global
  ├── Dönem: Aralık 2025
  ├── Şablon (Checklist): Seçilen form
  └── Başlangıç-Bitiş tarihi
```

---

### 5. ATAMA MANTIĞI

```
❌ ESKİ (YANLIŞ): Atama = Tek görev, kapanır
✅ YENİ (DOĞRU): Atama = Checklist atanır → Kullanıcı istediği kadar değerlendirme yapar

Atama (Assignment)
  └── Değerlendirme 1 ✓
  └── Değerlendirme 2 ✓
  └── Değerlendirme 3 ○
  └── ... (Atama açık kalır)
```

---

## YENİ ENTITY TASARIMI

### Yeni Entity'ler

#### CustomerOrganization
```csharp
public class CustomerOrganization : BaseEntity
{
    public string Name { get; set; }
    public string? Code { get; set; }
    public int CustomerId { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
}
```

#### CustomerOrganizationManager (Many-to-Many)
```csharp
// Bir personel birden çok organizasyonu yönetebilir
public class CustomerOrganizationManager : BaseEntity
{
    public int CustomerPersonnelId { get; set; }
    public int CustomerOrganizationId { get; set; }
    public bool IsPrimary { get; set; }
}
```

#### CustomerPersonnelOrganizationAccess (Many-to-Many)
```csharp
// Değerlendirici birden çok organizasyonu değerlendirebilir
public class CustomerPersonnelOrganizationAccess : BaseEntity
{
    public int CustomerPersonnelId { get; set; }
    public int CustomerOrganizationId { get; set; }
    public bool CanEvaluate { get; set; }
}
```

### Enum Değişiklikleri

#### UserRole (5 → 3 rol)
```csharp
public enum UserRole
{
    Admin = 1,
    QualitySpecialist = 2,  // Kalite Uzmanı
    FieldWorker = 3         // Saha Çalışanı
}
```

#### CustomerPersonnelRole (4 → 3 rol)
```csharp
public enum CustomerPersonnelRole
{
    CustomerManager = 1,     // Müşteri Yöneticisi
    CustomerSupervisor = 2,  // Müşteri Süpervizörü (Takım Lideri)
    CustomerOperator = 3     // Müşteri Operatörü
    // CustomerViewer, Expert, FieldWorker KALDIRILDI
}
```

### Entity Güncellemeleri

#### CustomerPersonnel - Yeni Alanlar
```csharp
// Takım Lideri ilişkisi
public int? SupervisorId { get; set; }
public CustomerPersonnel? Supervisor { get; set; }
public ICollection<CustomerPersonnel> TeamMembers { get; set; }

// Organizasyon bağlantısı
public int? OrganizationId { get; set; }
public CustomerOrganization? Organization { get; set; }

// Yönettiği organizasyonlar
public ICollection<CustomerOrganizationManager> ManagedOrganizations { get; set; }

// Değerlendirebileceği organizasyonlar
public ICollection<CustomerPersonnelOrganizationAccess> OrganizationAccess { get; set; }
```

#### Evaluation - Yeni Alanlar
```csharp
// Değerlendirilen organizasyon
public int? EvaluatedOrganizationId { get; set; }
public CustomerOrganization? EvaluatedOrganization { get; set; }

// Değerlendirilen müşteri personeli
public int? EvaluatedCustomerPersonnelId { get; set; }
public CustomerPersonnel? EvaluatedCustomerPersonnel { get; set; }
```

---

## DASHBOARD İHTİYAÇLARI

| Metrik | Açıklama |
|--------|----------|
| Günlük dinleme | Bugün kaç çağrı dinlendi |
| Kişi bazlı | Bahar bugün 23 dinleme yaptı |
| Hedef takibi | Günlük hedef: 55 dinleme |
| Temsilci bazlı | Her temsilciden 15 çağrı hedefi |
| Proje durumu | Kaç dinlenmiş, kaç kalmış |

---

## KALDIRILACAKLAR

- ✅ ~~Görüntüleyici rolü (CustomerViewer)~~ KALDIRILDI
- ❌ Personel menüsü (hiyerarşi içine taşınacak)
- ✅ ~~Eski UserRole'ler (TeamLeader, Evaluator, CustomerRepresentative, FieldWorker)~~ KALDIRILDI
- ✅ ~~Branch sistemi (Şubeler)~~ KALDIRILDI - 30 Aralık 2025
- ✅ ~~FieldWorker entity~~ KALDIRILDI - 31 Aralık 2025
- ✅ ~~Organization modülü (OrganizationUnit, Delegation)~~ KALDIRILDI - 31 Aralık 2025
- ✅ ~~Visit modülü (VisitDetails, CustomerVisit)~~ KALDIRILDI - 31 Aralık 2025
- ✅ ~~Calls modülü~~ KALDIRILDI - 31 Aralık 2025
- ✅ ~~İşlemler menüsü~~ KALDIRILDI - 31 Aralık 2025

---

## YAPILACAKLAR

### Öncelik 1 - Entity Değişiklikleri ✅ TAMAMLANDI
- [x] UserRole enum güncelle (3 rol: Admin, QualitySpecialist, FieldWorker)
- [x] CustomerPersonnelRole enum güncelle (3 rol: Manager, Supervisor, Operator)
- [x] CustomerOrganization entity oluştur
- [x] CustomerOrganizationManager entity oluştur
- [x] CustomerPersonnelOrganizationAccess entity oluştur
- [x] CustomerPersonnel'e yeni alanlar ekle (SupervisorId, OrganizationId, vb.)
- [x] Evaluation'a yeni alanlar ekle (EvaluatedCustomerPersonnelId, EvaluatedOrganizationId)
- [x] Migration oluştur (CustomerOrganizationHierarchy)
- [x] Frontend dosyalarını güncelle (roller, localization, sidebar)

### Öncelik 1.5 - Branch Sistemi Kaldırma ✅ TAMAMLANDI (30 Aralık 2025)
- [x] Branch entity tamamen kaldırıldı
- [x] ProjectBranch entity kaldırıldı
- [x] Tüm entity'lerden BranchId ve Branch ilişkileri kaldırıldı (User, Assignment, Call, CustomerVisit, Personnel, OrganizationUnit)
- [x] Branch repository, service, controller ve view'ler silindi
- [x] Branch DTO'lar silindi
- [x] ApplicationDbContext ve Program.cs güncellendi
- [x] `RemoveBranchSystem` migration oluşturuldu ve uygulandı
- [x] Sidebar'dan "Şubeler" menüsü kaldırıldı

### Öncelik 2 - UI Değişiklikleri
- [x] Organizasyon yönetimi ekranı (CustomerOrganization CRUD) ✅ TAMAMLANDI
- [x] Personel-Organizasyon ilişkilendirme ekranı ✅ TAMAMLANDI
- [x] Hiyerarşi görünümü (tree yapısı) ✅ TAMAMLANDI (1 Ocak 2026)
- [x] Değerlendirme akışını güncelle (önce org seç, sonra personel) ✅ TAMAMLANDI (1 Ocak 2026)

### Öncelik 3 - Dashboard
- [x] Günlük dinleme metrikleri ✅ TAMAMLANDI (1 Ocak 2026)
- [x] Kişi bazlı performans ✅ TAMAMLANDI (1 Ocak 2026)
- [x] Hedef takibi ✅ TAMAMLANDI (1 Ocak 2026)

---

## Video Transkript Dosyaları

- `Storage/ScreenShots/video1_transcript.txt`
- `Storage/ScreenShots/video2_transcript.txt`

---

## Teknik Notlar

### Database
- Database tazelenebilir (yeni migration ile)
- Tüm eski veriler silinecek

### Build Durumu
- Son build: 0 Error, 0 Warning
- Branch sistemi kaldırıldı ve `RemoveBranchSystem` migration uygulandı (30 Aralık 2025)

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

## ⚠️ TEST EDİLECEKLER

1. **Taslak Düzenleme** - Kalem ikonuna basınca sorular ve cevaplar geliyor mu?
2. **Değerlendirme Tamamlama** - Submit'ten sonra özet görünüyor mu?
3. **Detay Modalı** - Tamamlanmış değerlendirmeye tıklayınca veriler görünüyor mu?

---

## 📋 YARIN YAPILACAKLAR (9 Ocak 2026)

1. **Puanlama Sistemi Yeniden Düzenlenecek**

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

## ⚠️ DÜZELTILECEK: Permission Sistemi (8 Ocak 2026)

### Mevcut Durum - SORUNLU
**Permission sistemi şu an "dekoratif" - hiçbir şeyi kontrol etmiyor!**

```
Database'deki Permission tablosu:
┌─────────────────────────────────┐
│ Code: "Evaluations.View"        │  ← Sadece etiket
│ DisplayName: "Değerlendirme..." │  ← Sadece görsel
└─────────────────────────────────┘
         │
         │ HİÇBİR BAĞLANTI YOK ❌
         ▼
Controller'daki gerçek kontrol:
┌─────────────────────────────────┐
│ [Authorize(Roles = "Admin")]    │  ← Hardcoded rol kontrolü
└─────────────────────────────────┘
```

### Yapılan İşler (8 Ocak 2026)
- ✅ PermissionCategory enum'a yeni kategoriler eklendi (Languages, Trainings, Meetings, Approvals, DraftRequests, CustomerOrganizations, Personnel)
- ✅ SeedData.cs'e 55 yeni permission tanımı eklendi
- ✅ `SyncPermissionsAsync` metodu oluşturuldu (mevcut DB'ye yeni permission'ları ekler)
- ✅ `POST /api/permissions/sync` endpoint eklendi
- ✅ Permissions sayfasına "Yetkileri Senkronize Et" butonu eklendi

### Yapılması Gereken
1. **Policy tabanlı authorization'a geçiş:**
   - `[Authorize(Roles = "Admin")]` → `[Authorize(Policy = "Users.View")]`
   - Program.cs'de policy'leri database'den okuyacak handler yazılması

2. **CompanyUsers (CustomerPersonnel) için ayrı sistem:**
   - CustomerPersonnel'in permission sistemiyle HİÇBİR ilgisi yok
   - Onlar için ayrı sayfalar/portal gerekecek
   - Şu anki permission sistemi sadece User (bizim şirket çalışanları) için

### Neden Önemli?
- CompanyUsers (müşteri tarafı) sisteme girdiğinde ne görecekleri belirsiz
- Şu an herkes her şeyi görebiliyor (rol bazlı kısıtlı)
- Granüler yetki kontrolü YOK
