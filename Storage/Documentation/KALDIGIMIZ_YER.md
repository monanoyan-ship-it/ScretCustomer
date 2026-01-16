# Kaldığımız Yer - 16 Ocak 2026 (Son Güncelleme - 2)

---

## ✅ TAMAMLANAN İŞLER (16 Ocak 2026 - Oturum 2)

### CSV/Excel ile Dış Katılımcı Email Listesi Yükleme

Dış katılımcılar için dosyadan email listesi yükleme özelliği eklendi.

**Yeni Endpoint'ler:**
- `POST /api/surveys/{projectId}/upload-external-emails` - CSV/Excel dosyası yükle ve davetiye gönder
- `GET /api/surveys/external-email-template?format=csv|xlsx` - Şablon dosyası indir

**Desteklenen Dosya Formatları:**
- CSV (virgül, noktalı virgül veya tab ile ayrılmış)
- Excel (.xlsx, .xls)

**Kolon Eşleştirme (Esnek):**
| Kolon | Alternatif İsimler |
|-------|-------------------|
| Email | email, e-mail, mail, eposta, e-posta |
| Ad | firstname, ad, first_name, isim |
| Soyad | lastname, soyad, last_name, soyisim |
| Ad Soyad | fullname, adsoyad, ad soyad, name |

**UI Değişiklikleri:**
- Projects modal "Dış Katılımcılar" tabına dosya yükleme bölümü eklendi
- Şablon indirme linkleri (CSV ve Excel)
- Yükleme sonucu özeti (toplam, eklenen, gönderilen, mükerrer)

**Dosyalar:**
- `SurveyApiController.cs` - Upload endpoint, ParseCsvFileAsync, ParseExcelFile metodları
- `wwwroot/js/Projects/Index.js` - uploadExternalEmails, downloadExternalEmailTemplate fonksiyonları
- `Views/Projects/Index.cshtml` - Dosya yükleme UI

---

## ✅ TAMAMLANAN İŞLER (16 Ocak 2026 - Oturum 1)

### Dış Katılımcı Davetiye Sistemi (SurveyExternalInvitation)

Email listesine açık anket davetiyesi gönderme özelliği eklendi. Artık CustomerPersonnel dışındaki kişilere de anket gönderilebilir.

**Yeni Entity: `SurveyExternalInvitation`**
```csharp
public class SurveyExternalInvitation : BaseEntity
{
    public int ProjectId { get; set; }
    public string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Token { get; set; }  // GUID
    public int StatusId { get; set; }  // SurveyInvitationStatuses
    // ... IsOpened, IsCompleted, EvaluationId vb.
}
```

**İki Farklı Davet Sistemi:**
| | **SurveyInvitation** | **SurveyExternalInvitation** |
|---|---|---|
| **Hedef** | Müşteri personelleri (CustomerPersonnel) | Dış email listesi |
| **Token** | Encrypted (projectId:personnelId:timestamp) | GUID (32 karakter) |
| **Personel** | Zorunlu (CustomerPersonnelId) | Yok (sadece email + opsiyonel ad-soyad) |
| **UI Tab** | "Personel Davetiyeleri" | "Dış Katılımcılar" |

**Yeni API Endpoint'leri:**
- `POST /api/surveys/{projectId}/send-external-invitations` - Email listesine gönder
- `GET /api/surveys/{projectId}/external-invitations` - Davetiye listesi
- `GET /api/surveys/{projectId}/external-invitation-stats` - İstatistikler
- `POST /api/surveys/{projectId}/retry-external-failed` - Başarısızları tekrar gönder

**Email Giriş Formatı:**
```
email1@example.com
email2@example.com; Ahmet Yılmaz
email3@example.com, email4@example.com
```
- Virgül, boşluk veya satır sonu ile ayırma
- Opsiyonel ad-soyad: `email@x.com; Ad Soyad`

**UI Değişiklikleri:**
- Proje modal'a tab sistemi eklendi (Personel Davetiyeleri / Dış Katılımcılar)
- `Views/Projects/Index.cshtml` güncellendi
- `wwwroot/js/Projects/Index.js` güncellendi

---

### SurveyInvitationStatuses TypeItem'a Taşındı

`SurveyInvitationStatus` string const class'ı `SurveyInvitationStatuses` TypeItem'a dönüştürüldü.

**Önceki (string):**
```csharp
public string Status { get; set; } = SurveyInvitationStatus.Pending; // "Pending"
```

**Sonraki (int):**
```csharp
public int StatusId { get; set; } = SurveyInvitationStatuses.Ids.Pending; // 1
```

**TypeDefinitions.cs'e eklenen:**
```csharp
public static class SurveyInvitationStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", ...);
    public static readonly TypeItem Sent = new(2, "Sent", ...);
    public static readonly TypeItem Failed = new(3, "Failed", ...);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Sent = 2;
        public const int Failed = 3;
    }
}
```

**Migration:** `ChangeStatusToStatusId` - Veri dönüşümü SQL'leri ile

---

### Survey Token Validation Hataya Toleranslı

Test email'lerinden gelen linkler çalışmıyordu çünkü SurveyInvitations tablosu yoktu veya kayıt bulunamıyordu.

**Düzeltme:**
- ValidateToken ve SubmitSurvey'deki SurveyInvitation sorguları try-catch içine alındı
- Tablo/kayıt yoksa sessizce geçiyor, anket yine de çalışıyor
- Hata sadece loglanıyor

---

### CustomerPortal branch → project Terminolojisi

Tüm "branch" referansları "project" olarak güncellendi:
- `branches.js` → `projects.js` (rename)
- API endpoint'leri: `/api/customer/portal/branches` → `/api/customer/portal/projects`
- DTO'lar: `branchCount` → `projectCount`
- View binding'ler güncellendi

---

### Yeni Modüller (Önceki oturumdan)

**PerformanceSettings:**
- Proje tipi bazlı hedef ve eşik değer ayarları
- `/Settings/PerformanceSettings` sayfası
- `/api/performance-settings` API

**SupportRequest:**
- Kullanıcı destek talep/hata bildirim modülü
- `/SupportRequests/Index` sayfası
- Widget: `_SupportRequestWidget.cshtml`

---

## 📝 YAPILACAKLAR (Backlog)

- [ ] PDF çıktısı için microservice altyapısı (Docker container, LibreOffice/wkhtmltopdf)
- [ ] SurveyInvitations tablosu için migration uygulama kontrolü

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

## Teknik Notlar

### Database
- Migration'lar güncel
- Son migration: `ChangeStatusToStatusId`

### Build Durumu
- Son build: 0 Error, 12 Warning (nullable reference uyarıları)

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

## Video Transkript Dosyaları

- `Storage/ScreenShots/video1_transcript.txt`
- `Storage/ScreenShots/video2_transcript.txt`
