# Değerlendirme Bildirimi Yeniden Tasarım

## Mevcut Durum (TAMAMLANDI)
Çoklu bildirim kuralı implementasyonu tamamlandı.

## Entity: CustomerNotificationRule
- Id, CustomerId (FK)
- FrequencyId: 1=Her Kayıtta, 2=Günlük, 3=Haftalık, 4=Aylık
- DayOfWeek: int? (Haftalıksa: 1=Pzt...7=Pzr)
- DayOfMonth: int? (Aylıksa: 1-28)
- Emails: string (virgülle ayrılmış)
- SendToPersonnel: bool (personelin kendi mailine gidecek)
- EmailTemplateId: int? (FK → EmailTemplate, kural bazında şablon)
- IsActive: bool
- LastSentAt: DateTime?

## Token Sistemi (AES, DB'siz)
Payload: { type, evaluationId?, customerId?, customerPersonnelId?, startDate?, endDate?, expiresAt }
JSON → AES encrypt → Base64 URL-safe → link

## 3 Tip Link (Public, login gerektirmez)
1. **Tekil** (type=single): EvaluationId → tek dinleme detayı
2. **Toplu** (type=bulk): CustomerId + tarih aralığı → ExternalEvaluations benzeri liste
3. **Personel** (type=personnel): CustomerPersonnelId + tarih aralığı → MyPerformance benzeri

## Gönderim Mantığı
- **Her Kayıtta**: Değerlendirme tamamlanınca anında
- **Günlük**: Her akşam 19:00. Tarih: bugünün değerlendirmeleri
- **Haftalık**: Belirtilen gün 19:00. Tarih: son 1 hafta
- **Aylık**: Belirtilen gün 19:00. Tarih: son 1 ay

## Key Dosyalar
- `CustomerNotificationRule.cs` entity + migration
- `NotificationTokenService.cs` (AES token encrypt/decrypt)
- `EvaluationNotificationService.cs` (rule-based: per-eval/daily/weekly/monthly)
- `EvaluationNotificationJob.cs` (19:00 Turkey time)
- `PublicReportApiController.cs` + `EvaluationReportController.cs` + view + JS
- `Customers/Index.cshtml` + `customers.js` (dynamic rules UI)
- `appsettings.json` → AppUrl, NotificationToken config

## SignalR Bildirim Sistemi (TAMAMLANDI)
- `API/Hubs/NotificationHub.cs` - [Authorize] hub, user_{userId} group pattern
- `Services/Services/NotificationCreatorService.cs` - DB + push + email
- `API/wwwroot/js/Shared/notification-bell.js` - Bell dropdown + SignalR + toastr
- 15 trigger noktası: Assignment (create/complete/cancel), Project (team/complete/cancel), Evaluation (complete/revert/cancel), Approvals, FieldWorker, PersonnelRequest, SupportRequest, Announcements
