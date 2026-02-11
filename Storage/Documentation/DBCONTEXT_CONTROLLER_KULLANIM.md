# Controller'larda Direkt DbContext Kullanımı

> **Sorun:** Controller'lar `ApplicationDbContext`'i direkt inject edip veritabanına erişiyor.
> Doğru mimari: Controller → Service → DbContext

## Özet

**17 controller'da** `ApplicationDbContext` direkt inject edilmiş ve **hepsi aktif olarak kullanıyor**.
Bu controller'lardaki `_context` kullanımlarının service katmanına taşınması gerekiyor.

## Etkilenen Controller'lar (17)

1. `Controllers/DashboardController.cs`
2. `Controllers/CustomerPortalController.cs` (MVC)
3. `Controllers/Api/AnnouncementsApiController.cs`
4. `Controllers/Api/ApprovalsApiController.cs`
5. `Controllers/Api/ChecklistsApiController.cs`
6. `Controllers/Api/CustomerPortalController.cs` (API)
7. `Controllers/Api/CustomerPortalProfileController.cs`
8. `Controllers/Api/DashboardApiController.cs`
9. `Controllers/Api/EmailTemplatesApiController.cs`
10. `Controllers/Api/EvaluationsApiController.cs`
11. `Controllers/Api/NotificationsApiController.cs`
12. `Controllers/Api/PermissionsApiController.cs`
13. `Controllers/Api/ProjectFilesApiController.cs`
14. `Controllers/Api/PublicReportApiController.cs`
15. `Controllers/Api/QuestionAttachmentsApiController.cs`
16. `Controllers/Api/SmtpApiController.cs`
17. `Controllers/Api/SurveyApiController.cs`

## Yapılması Gereken

Her controller için:
1. `_context` kullanımlarını ilgili service'e taşı (veya yeni service oluştur)
2. Controller'dan `ApplicationDbContext` inject'ini kaldır
3. Controller sadece service'i çağırsın

> **NOT:** Bu büyük bir refactoring görevi. Her controller tek tek ele alınmalı.
