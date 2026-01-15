# Proje Yapisi

Bu dokuman, SecretCustomer projesinin teknik mimarisini ve dosya yapisini detayli olarak aciklar.

## Genel Bakis

```
ScretCustomer/
├── Backend/                          # .NET Backend
│   ├── SecretCustomer.API/           # Web katmani (Controllers, Views, wwwroot)
│   ├── SecretCustomer.Core/          # Domain katmani (Entities, DTOs, Interfaces)
│   ├── SecretCustomer.Data/          # Data katmani (DbContext, Migrations, Configurations)
│   └── SecretCustomer.Services/      # Business katmani (Services)
├── docs/                             # Proje dokumantasyonu
├── Documentation/                    # Ek dokumantasyon
└── ExcelService/                     # Excel isleme servisi (Python)
```

## Backend Katmanlari

### 1. SecretCustomer.Core (Domain Layer)

Domain mantigi ve tum katmanlarin kullandigi ortak tanimlamalari icerir.

```
SecretCustomer.Core/
├── Entities/                         # Domain nesneleri
│   ├── BaseEntity.cs                 # Tum entity'lerin base class'i
│   ├── User.cs                       # Sistem kullanicilari
│   ├── Personnel.cs                  # Saha calisanlari
│   ├── Customer.cs                   # Musteriler
│   ├── CustomerPersonnel.cs          # Musteri personelleri
│   ├── CustomerOrganization.cs       # Musteri organizasyonlari (sube/bolge)
│   ├── CustomerPersonnelOrganization.cs  # Personel-Organizasyon iliskisi (junction)
│   ├── Project.cs                    # Projeler
│   ├── ProjectFile.cs                # Proje dosyalari
│   ├── Checklist.cs                  # Kontrol listeleri
│   ├── Question.cs                   # Sorular
│   ├── QuestionSubCriteria.cs        # Alt kriterler
│   ├── QuestionAttachment.cs         # Soru ekleri
│   ├── Assignment.cs                 # Atamalar
│   ├── AssignmentPeriod.cs           # Atama donemleri
│   ├── Evaluation.cs                 # Degerlendirmeler
│   ├── Answer.cs                     # Cevaplar
│   ├── AnswerSubCriteriaSelection.cs # Alt kriter secimleri
│   ├── Training.cs                   # Egitimler
│   ├── Meeting.cs                    # Toplantiar
│   ├── Approval.cs                   # Onaylar
│   ├── Permission.cs                 # Izinler
│   ├── RolePermission.cs             # Rol izinleri
│   ├── UserPermission.cs             # Kullanici izinleri
│   ├── CustomerPersonnelPermission.cs # Musteri personel izinleri
│   ├── PersonnelRequest.cs           # Personel talepleri
│   ├── ExcelTemplate.cs              # Excel sablonlari
│   ├── ExcelColumn.cs                # Excel kolonlari
│   ├── Language.cs                   # Diller
│   ├── LocaleStringResource.cs       # Ceviri kaynaklari
│   ├── SystemSetting.cs              # Sistem ayarlari
│   ├── AppSettings.cs                # Uygulama ayarlari
│   ├── AuditLog.cs                   # Denetim kayitlari
│   ├── Announcement.cs               # Duyurular
│   └── CustomerTaskList.cs           # Gorev listeleri
├── DTOs/                             # Data Transfer Objects
│   ├── Auth/                         # Kimlik dogrulama DTO'lari
│   ├── User/                         # Kullanici DTO'lari
│   ├── Customer/                     # Musteri DTO'lari
│   ├── CustomerOrganization/         # Organizasyon DTO'lari
│   ├── Personnel/                    # Personel DTO'lari
│   ├── Project/                      # Proje DTO'lari
│   ├── Checklist/                    # Kontrol listesi DTO'lari
│   ├── Question/                     # Soru DTO'lari
│   ├── Assignment/                   # Atama DTO'lari
│   ├── Evaluation/                   # Degerlendirme DTO'lari
│   ├── Report/                       # Rapor DTO'lari
│   ├── Dashboard/                    # Dashboard DTO'lari
│   ├── Import/                       # Import DTO'lari
│   └── ...
├── Interfaces/
│   ├── Repositories/                 # Repository arayuzleri
│   │   └── IGenericRepository.cs
│   └── Services/                     # Servis arayuzleri
│       ├── IAuthService.cs
│       ├── IUserService.cs
│       ├── ICustomerService.cs
│       ├── ICustomerPersonnelService.cs
│       ├── ICustomerOrganizationService.cs
│       ├── IProjectService.cs
│       ├── IChecklistService.cs
│       ├── IAssignmentService.cs
│       ├── IEvaluationService.cs
│       ├── IReportService.cs
│       ├── IDashboardService.cs
│       ├── IPersonnelService.cs
│       ├── IPermissionService.cs
│       ├── IImportService.cs
│       ├── ILocalizationService.cs
│       ├── IFileUploadService.cs
│       ├── IExcelTemplateService.cs
│       ├── IAuditLogService.cs
│       ├── ISystemSettingService.cs
│       ├── IAppSettingsService.cs
│       ├── IPersonnelRequestService.cs
│       └── IQRCodeService.cs
└── Enums/                            # Enum tanimlamalari
```

### 2. SecretCustomer.Data (Data Access Layer)

Veritabani erisimi ve EF Core konfigurasyonlarini icerir.

```
SecretCustomer.Data/
├── ApplicationDbContext.cs           # EF Core DbContext
├── Migrations/                        # EF Core migration'lari
├── Configurations/                    # Entity konfigurasyonlari
│   ├── UserConfiguration.cs
│   ├── CustomerConfiguration.cs
│   └── ...
└── Repositories/                      # Repository implementasyonlari
    └── GenericRepository.cs
```

### 3. SecretCustomer.Services (Business Layer)

Is mantigi ve servis implementasyonlarini icerir.

```
SecretCustomer.Services/
└── Services/
    ├── AuthService.cs                 # Kimlik dogrulama
    ├── UserService.cs                 # Kullanici islemleri
    ├── CustomerService.cs             # Musteri islemleri
    ├── CustomerPersonnelService.cs    # Musteri personel islemleri
    ├── CustomerOrganizationService.cs # Organizasyon islemleri
    ├── ProjectService.cs              # Proje islemleri
    ├── ChecklistService.cs            # Kontrol listesi islemleri
    ├── AssignmentService.cs           # Atama islemleri
    ├── EvaluationService.cs           # Degerlendirme islemleri
    ├── ReportService.cs               # Rapor islemleri
    ├── DashboardService.cs            # Dashboard islemleri
    ├── PersonnelService.cs            # Saha personeli islemleri
    ├── PermissionService.cs           # Yetki islemleri
    ├── ImportService.cs               # Import islemleri
    ├── LocalizationService.cs         # Ceviri islemleri
    ├── FileUploadService.cs           # Dosya yukleme
    ├── ExcelTemplateService.cs        # Excel sablon islemleri
    ├── AuditLogService.cs             # Denetim kaydi
    ├── SystemSettingService.cs        # Sistem ayarlari
    ├── AppSettingsService.cs          # Uygulama ayarlari
    ├── PersonnelRequestService.cs     # Personel talepleri
    └── QRCodeService.cs               # QR kod olusturma
```

### 4. SecretCustomer.API (Presentation Layer)

Web uygulamasi katmani - MVC + API yapisi.

```
SecretCustomer.API/
├── Controllers/                       # MVC Controller'lar (Views doner)
│   ├── HomeController.cs
│   ├── AccountController.cs           # Login/Logout
│   ├── DashboardController.cs         # Admin/User dashboard
│   ├── CustomersController.cs
│   ├── CustomerPersonnelController.cs
│   ├── CustomerOrganizationsController.cs
│   ├── ProjectsController.cs
│   ├── ChecklistsController.cs
│   ├── AssignmentsController.cs
│   ├── EvaluationsController.cs
│   ├── ListeningsController.cs
│   ├── ReportsController.cs
│   ├── PersonnelController.cs
│   ├── UsersController.cs
│   ├── PermissionsController.cs
│   ├── TrainingsController.cs
│   ├── MeetingsController.cs
│   ├── ApprovalsController.cs
│   ├── SettingsController.cs
│   ├── LanguagesController.cs
│   ├── ImportController.cs
│   ├── ExcelTemplatesController.cs
│   ├── AuditLogsController.cs
│   ├── NotificationsController.cs
│   ├── ProfileController.cs
│   ├── FormController.cs              # Public form (QR ile erisim)
│   ├── MyAssignmentsController.cs
│   ├── InternalAssignmentsController.cs
│   ├── UserRequestsController.cs
│   └── CustomerPortalController.cs    # Musteri portali MVC
│
├── Controllers/Api/                   # API Controller'lar (JSON doner)
│   ├── BaseApiController.cs           # Ortak API base class
│   ├── AuthController.cs
│   ├── UsersApiController.cs
│   ├── CustomersApiController.cs
│   ├── CustomerPersonnelApiController.cs
│   ├── CustomerOrganizationsApiController.cs
│   ├── ProjectsApiController.cs
│   ├── ChecklistsApiController.cs
│   ├── AssignmentsApiController.cs
│   ├── InternalAssignmentsApiController.cs
│   ├── EvaluationsApiController.cs
│   ├── ReportsApiController.cs
│   ├── DashboardApiController.cs
│   ├── PersonnelApiController.cs
│   ├── PermissionsApiController.cs
│   ├── TrainingsApiController.cs
│   ├── MeetingsApiController.cs
│   ├── ApprovalsApiController.cs
│   ├── ImportApiController.cs
│   ├── ExcelTemplatesApiController.cs
│   ├── LocalizationApiController.cs
│   ├── NotificationsApiController.cs
│   ├── ProfileApiController.cs
│   ├── AuditLogsApiController.cs
│   ├── EnumsApiController.cs
│   ├── AnswersApiController.cs
│   ├── QuestionAttachmentsApiController.cs
│   ├── PersonnelRequestsApiController.cs
│   ├── ProjectFilesApiController.cs
│   ├── AnnouncementsApiController.cs
│   ├── SystemSettingsApiController.cs
│   ├── AppSettingsApiController.cs
│   ├── CustomerPortalAuthController.cs    # Musteri portali auth
│   ├── CustomerPortalController.cs        # Musteri portali API
│   └── CustomerPortalProfileController.cs # Musteri portali profil
│
├── Views/                             # Razor Views
│   ├── Shared/
│   │   ├── _Layout.cshtml             # Ana layout (admin panel)
│   │   ├── _CustomerLayout.cshtml     # Musteri portali layout
│   │   ├── _Sidebar.cshtml            # Sol menu
│   │   ├── _TopNav.cshtml             # Ust bar
│   │   ├── _DeleteConfirmationModal.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Account/                       # Login, password reset
│   ├── Dashboard/                     # Dashboard sayfalar
│   ├── Customers/
│   ├── CustomerPersonnel/
│   ├── CustomerOrganizations/
│   ├── Projects/
│   ├── Checklists/
│   ├── Assignments/
│   ├── Evaluations/
│   ├── Listenings/
│   ├── Reports/
│   ├── Personnel/
│   ├── Users/
│   ├── Permissions/
│   ├── Trainings/
│   ├── Meetings/
│   ├── Approvals/
│   ├── Settings/
│   ├── Languages/
│   ├── Import/
│   ├── ExcelTemplates/
│   ├── AuditLogs/
│   ├── Notifications/
│   ├── Profile/
│   ├── Form/
│   ├── MyAssignments/
│   ├── InternalAssignments/
│   ├── UserRequests/
│   └── CustomerPortal/                # Musteri portali sayfalari
│       ├── Dashboard.cshtml
│       ├── Evaluations.cshtml
│       ├── Reports.cshtml
│       ├── Branches.cshtml
│       └── Profile.cshtml
│
├── wwwroot/                           # Static dosyalar
│   ├── css/
│   │   ├── site.css                   # Ana stiller
│   │   └── customer-portal.css        # Musteri portali stilleri
│   ├── js/
│   │   ├── sidebar.js
│   │   ├── Shared/                    # Paylasilan JS servisleri
│   │   │   ├── api.service.js         # API cagrilari
│   │   │   ├── customer.api.service.js # Musteri portali API
│   │   │   ├── auth.service.js        # Auth islemleri
│   │   │   ├── enums.service.js       # Enum islemleri
│   │   │   ├── localization.js        # Ceviri islemleri
│   │   │   ├── app.config.js          # Uygulama konfigurasyon
│   │   │   ├── table-sorting.js       # Tablo siralama
│   │   │   ├── delete-confirmation.js # Silme onay modal
│   │   │   └── confirm-modal.js       # Genel onay modal
│   │   ├── Dashboard/
│   │   ├── Customers/
│   │   ├── CustomerPortal/            # Musteri portali JS
│   │   │   ├── dashboard.js
│   │   │   ├── evaluations.js
│   │   │   ├── reports.js
│   │   │   ├── branches.js
│   │   │   └── profile.js
│   │   ├── Evaluations/
│   │   ├── Listenings/
│   │   ├── Reports/
│   │   ├── Personnel/
│   │   ├── Users/
│   │   ├── Projects/
│   │   ├── Checklists/
│   │   ├── Assignments/
│   │   └── ...
│   └── lib/                           # 3rd party kutuphaneler
│       ├── bootstrap/
│       ├── jquery/
│       └── knockout/
│
├── DTOs/                              # API-specific DTO'lar
│   ├── ExcelTemplateDto.cs
│   └── ExcelColumnDto.cs
│
├── Helpers/                           # Yardimci siniflar
│   ├── HtmlLocalizer.cs               # HTML ceviri helper
│   └── ...
│
├── Program.cs                         # Uygulama giris noktasi
├── appsettings.json                   # Konfigurasyon
└── appsettings.Development.json       # Development konfig
```

## Kullanici Rolleri

### Sistem Rolleri (User tablosu)
| Rol | Aciklama |
|-----|----------|
| Admin | Tam yetki, tum modullere erisim |
| TeamLeader | Takim yoneticisi, kendi takimi icin islemler |
| Evaluator | Degerlendirici, form doldurma |
| CustomerRepresentative | Musteri temsilcisi |

### Musteri Portali Rolleri (CustomerPersonnel tablosu)
| Rol | Aciklama | Veri Erisimi |
|-----|----------|--------------|
| CustomerManager | Müşteri Yöneticisi | Tüm organizasyonlar |
| CustomerSupervisor | Takım Lideri | Kendi takımı veya atandığı organizasyon |
| CustomerOperator | Operatör | Sadece kendisi |

## Veritabani Iliskileri

### Temel Iliski Diyagrami

```
Customer 1--* CustomerPersonnel
Customer 1--* CustomerOrganization
CustomerPersonnel *--* CustomerOrganization (via CustomerPersonnelOrganization)
CustomerPersonnelOrganization *--1 CustomerPersonnel (Supervisor)

Project *--1 Customer
Project *--1 Checklist
Project 1--* Assignment

Checklist 1--* Question
Question 1--* QuestionSubCriteria
Question 1--* QuestionAttachment

Assignment *--1 Personnel (Evaluator)
Assignment *--1 CustomerPersonnel (EvaluatedPersonnel)
Assignment 1--1 Evaluation

Evaluation 1--* Answer
Answer *--1 Question
Answer 1--* AnswerSubCriteriaSelection
```

### CustomerPersonnelOrganization (Junction Table)
Musteri personeli ile organizasyon arasindaki many-to-many iliskiyi yonetir. Her atama icin ayri bir supervisor belirlenebilir.

```csharp
public class CustomerPersonnelOrganization
{
    public int Id { get; set; }
    public int CustomerPersonnelId { get; set; }
    public int CustomerOrganizationId { get; set; }
    public int? SupervisorId { get; set; }  // Bu organizasyon icin supervisor
}
```

## Frontend Mimarisi

### KnockoutJS MVVM Pattern

Her sayfa icin bir ViewModel sinifi olusturulur:

```javascript
function MyPageViewModel() {
    var self = this;

    // Observables
    self.items = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.selectedItem = ko.observable(null);

    // Computed
    self.hasItems = ko.computed(function() {
        return self.items().length > 0;
    });

    // Methods
    self.loadData = function() {
        self.isLoading(true);
        ApiService.get('/api/items')
            .then(function(data) {
                self.items(data);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Init
    self.loadData();
}

ko.applyBindings(new MyPageViewModel(), document.getElementById('my-page-app'));
```

### API Servisleri

**ApiService (Sistem):**
- Cookie-based auth kullanir
- `/api/*` endpoint'lerine istek atar

**CustomerApiService (Musteri Portali):**
- JWT token-based auth kullanir
- localStorage'da token saklar
- `/api/customer-portal/*` endpoint'lerine istek atar

## Kimlik Dogrulama

### Sistem Kullanicilari
- Cookie-based authentication
- ASP.NET Identity entegrasyonu
- `/Account/Login` ile giris

### Musteri Portali
- JWT token-based authentication
- `/api/customer-portal/auth/login` ile giris
- Token localStorage'da saklanir
- Her istekte `Authorization: Bearer {token}` header'i gonderilir

## Lokalizasyon

Coklu dil destegi icin:

**Backend:**
```csharp
@Html.T("Key.Name", "Default Value")
```

**Frontend:**
```javascript
localization.get('Key.Name', 'Default Value')
```

Ceviri kaynaklari `LocaleStringResource` tablosunda saklanir.

## Dosya Isleme

### Desteklenen Formatlar
- Excel (.xlsx) - Import/Export
- Resimler (.jpg, .png, .gif) - Ekler
- Ses dosyalari - Dinleme kayitlari

### Upload Dizini
`wwwroot/uploads/` altinda kategorilere gore saklanir.

## Raporlama

### Rapor Turleri
1. **Degerlendirme Raporlari** - Tamamlanan degerlendirmeler
2. **Performans Raporlari** - Personel performansi
3. **Trend Analizleri** - Zaman bazli trendler
4. **Karsilastirmali Raporlar** - Organizasyon karsilastirmasi

### Export Formatlari
- Excel (.xlsx)
- PDF (planlanmis)

## Guvenlik

### Yetki Kontrolleri
- Role-based authorization (`[Authorize(Roles = "Admin")]`)
- Permission-based authorization (Permission tablosu)
- Data-level filtering (kullanici sadece kendi verisini gorur)

### Veri Erisim Kurallari
- Admin: Tum veriler
- TeamLeader: Kendi takimi
- Evaluator: Kendisine atanan isler
- CustomerManager: Musteri altindaki tum veriler
- CustomerSupervisor: Kendi takimi veya atandigi organizasyon
- CustomerOperator: Sadece kendisi

## Sayfa Eslesmeleri (Admin Panel - Musteri Portali)

Admin panel ve Musteri Portali arasinda birebir eslesen sayfalar vardir. Bu sayfalar ayni islevi gorur, tek fark veri filtrelemesidir.

### Eslesen Sayfalar

| Admin Panel | Musteri Portali | Fark |
|-------------|-----------------|------|
| `/Evaluations/Index` | `/CustomerPortal/Evaluations` | Admin tum degerlendirmeleri, Musteri Portali rol bazli filtrelenmiş verileri gosterir |
| `/Listenings/Index` | `/CustomerPortal/Listenings` | Ayni mantik, dinleme kayitlari icin |
| `/Reports/Index` | `/CustomerPortal/Reports` | Ayni mantik, raporlar icin |
| `/Dashboard/Index` | `/CustomerPortal/Dashboard` | Ayni mantik, dashboard icin |

### Temel Fark - Kimlik Dogrulama

| | Admin Panel | Musteri Portali |
|--|-------------|-----------------|
| **Tablo** | User | CustomerPersonnel |
| **ID Alani** | UserId | CustomerPersonnelId |
| **Auth** | Cookie-based | JWT Token |
| **Veri Filtresi** | EvaluatorId, CreatedById vb. | EvaluatedCustomerPersonnelId |

### Gelistirme Kurallari

1. **Ayni UI Yapisi:** Eslesen sayfalar ayni tablo yapisi, modal icerigi ve filtreleme seceneklerini kullanmalidir
2. **Farkli Veri Kaynagi:**
   - Admin Panel: `UserId` uzerinden sorgular (User tablosu)
   - Musteri Portali: `CustomerPersonnelId` uzerinden sorgular (CustomerPersonnel tablosu)
3. **Kod Tutarliligi:** Bir sayfada yapilan UI degisikligi diger sayfaya da yansitilmalidir
4. **DTO Paylasimi:** Mumkun oldugunda ayni DTO'lar kullanilmalidir

### Sayfa Esitleme Durumu

| Sayfa | Admin Panel | CustomerPortal | Durum |
|-------|-------------|----------------|-------|
| Evaluations Detay Modal | 3 kart + 6 kolonlu tablo | 3 kart + 6 kolonlu tablo | ✓ Esitlendi |
| Listenings Detay Modal | 3 kart + 6 kolonlu tablo | - | Kontrol edilmeli |

### Detay Modal Yapisi

Her iki sayfa da su yapida olmali:

**Detay Modal - Info Kartlari:**
1. Degerlendirilen Bilgileri (Firma, Organizasyon, Degerlendirilen, Supervisor)
2. Cagri Bilgileri (Cagri ID, Tarih, Sure, Kontrol Saati)
3. Degerlendirme Bilgileri (Degerlendiren, Tamamlanma, Durum)

**Detay Modal - Cevap Tablosu:**
| Grup | Soru | Cevap | Agirlik | Kazanilan | Notlar |

**Veri Kaynagi Farki:**
- Admin: `EvaluationService` + `UserId` filtresi
- CustomerPortal: `CustomerPortalApiController` + `CustomerPersonnelId` filtresi

## Dokumantasyon Dizini

```
docs/
├── 01-proje-kurulumu.md
├── 02-database-modeli.md
├── 03-kontrol-listesi-modulu.md
├── 04-atama-mantigi-modulu.md
├── 05-dashboard-ve-raporlama.md
├── 06-authentication-authorization.md
├── 07-frontend-temel-yapi.md
├── 08-frontend-kontrol-listesi-ui.md
├── 09-frontend-degerlendirme-formu-ui.md
├── 10-frontend-ozet.md
├── 11-database-setup.md
├── 12-docker-deployment.md
├── 13-knockoutjs-kullanimi.md
├── 14-mimari-yaklasim-spa.md
├── 15-saha-calisanlari-modulu.md
├── 16-musteri-personel-modulu.md
├── 17-workflow-analysis-comparison.md
└── 18-proje-yapisi.md (bu dosya)

Documentation/
├── CLAUDE.md                          # Claude Code kurallari
├── DEVELOPMENT_PATTERNS.md            # Gelistirme kaliplari
├── KALDIGIMIZ_YER.md                  # Proje durumu
├── CSV_IMPORT_STANDARTLARI.md
├── is_kapsam_analizi.md
├── analiz_raporu_2025_12_13.md
└── PUSH_INSTRUCTIONS.md
```
