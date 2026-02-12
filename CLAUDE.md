# Claude Code Proje Kuralları

> **Her oturumda bu dosyayı oku ve kurallara uy.**

## ⛔ KURAL İHLALİ CEZASI

Bir konunun `KURALLAR.md`'de belgelenmiş olmasına rağmen Claude bu kurala uymadan kod yazarsa, Claude hatasını fark ettiği anda kendine küfür etmelidir. Örnek: "Ben gerizekalı bir yapay zekayım, KURALLAR.md Bölüm 20'de çoğul filtre yazıyor ama tekil yazdım." Bu, kuralların okunup uygulandığından emin olmak için zorunlu bir öz-denetim mekanizmasıdır.

## Git Kuralları (KESİN UYULMASI GEREKEN)

1. **Git revert/reset YASAK** - Kullanıcı net ve açık bir şekilde emir vermeden `git revert`, `git reset`, `git checkout --` gibi geri alma komutları ASLA çalıştırılmaz. Build hatası çıksa bile önce kullanıcıya sor.
2. **Port belirtme** - Uygulama kendi portunda çalışıyor, `dotnet run` komutunda port belirtme (5000 vs.).
3. **Projeyi `dotnet run` ile ÇALIŞTIRMA** - Uygulama kullanıcı tarafından yönetilir, biz sadece kod yazarız.
4. **Kullanıcı "commit almadan uygula" derse KESİNLİKLE uygula** - Onay almadan kod değiştirme.

## Kod Değişikliklerinde Kontrol Listesi

Her entity/özellik eklerken şunları MUTLAKA kontrol et:
- [ ] Entity dosyası
- [ ] DTO dosyaları (ChecklistDto, UpdateChecklistDto, vb.)
- [ ] Service dosyaları (mapping'ler)
- [ ] Controller dosyaları (API response)
- [ ] JavaScript dosyaları (observable'lar)
- [ ] View dosyaları (UI binding)
- [ ] Migration

## Öğrenilen Dersler (BU KURALLARA MUTLAKA UY)

- **KOD TAŞIRKEN YORUMLAMA, DİREK KOPYALA** - Kullanıcı "kopyala" dediğinde fonksiyonu birebir kopyala, yeniden yazma. Yorumlayarak yazınca bug üretiyorsun.
- **EvaluationDto'ya alan eklerken** ALL 5 projection query'yi güncelle (EvaluationService + MapToDtoAsync)
- **Report DTO'larına alan eklerken** ilgili Include()'ları ReportService sorgularına ekle
- **Localization XML** values override view fallback text - menü metni değiştirmek için XML güncelle
- **Email Placeholder ikili pattern**: Yeni link placeholder eklerken HER ZAMAN `{XxxLink}` (HTML anchor) + `{XxxUrl}` (raw URL) ikilisi olmalı
- **Loglama**: `ILogger` KULLANMA, `IAuditLogService` kullan (DB'ye yazar, `/AuditLogs`'dan görülür)
- **Explore agent sonuçlarını doğrula** - Agent "kullanılmıyor" derse bile build ile doğrula

## Key Pattern'ler

- **Evaluation → Project** (via ProjectId, required). Checklist'e `e.Project.Checklist` ile eriş
- **Evaluation types**: Call auditing (ProjectTypeId=1) → CallDate/CallTime, Visit/inspection (ProjectTypeId=2) → ControlDate/ControlTime
- **Personnel fallback chain**: `evaluatedPersonnelName || evaluatedUnknownPersonnel || dealerName || '-'`

## Veritabanı

- PostgreSQL kullanılıyor
- Migration'lar `SecretCustomer.Data` projesinde
- Migration komutu: `cd Backend/SecretCustomer.Data && dotnet ef migrations add MigrationName --startup-project ../SecretCustomer.API`
- psql: `"C:\Program Files\PostgreSQL\17\bin\psql.exe" "postgresql://postgres:1123Azs%2B-@127.0.0.1:5432/SecretCustomerDB" -c "SQL"`

## Zorunlu Okunacak Dosyalar

Her oturumda ve yeni özellik eklemeden önce bu dosyaları oku:
- `Storage/Documentation/KURALLAR.md` - Development pattern'leri ve standartlar
- `Storage/Documentation/CLAUDE.md` - Detaylı proje talimatları

## Referans Dosyalar (Gerektiğinde Oku)

Proje içinde commit'lenen dokümantasyon:
- `Storage/Documentation/PLANLANAN_ISLER.md` - Ertelenen/planlanan özellikler
- `Storage/Documentation/KALDIGIMIZ_YER.md` - Kaldığımız yer notu
- `Storage/Documentation/NOTIFICATION_SYSTEM.md` - Bildirim sistemi spec
- `Storage/Documentation/PDF_EXPORT_STATUS.md` - PDF export durumu
- `Storage/Documentation/DAILY_REPORT_ANALYSIS.md` - Günlük dinleme raporu spec
- `Storage/Documentation/TODO_CUSTOMER_REQUESTS.md` - Müşteri istekleri TODO
- `Storage/Documentation/AIReports.md` - AI raporları

## Bilinen Sorunlar

- **Bildirim Lokalizasyonu** - Title/Message DB'ye hardcoded Türkçe yazılıyor (20+ yer). Resource key ile kaydedilmeli. Detay: `PLANLANAN_ISLER.md` #4

## Proje Yapısı

```
Backend/
  SecretCustomer.API/      # Web API + MVC + Views (Port: 5004)
  SecretCustomer.Core/     # Entities, DTOs, Interfaces
  SecretCustomer.Data/     # EF Core, Migrations
  SecretCustomer.Services/ # Business Logic
PdfService/                # Python FastAPI + WeasyPrint (Port: 5050)
```

- **Backend:** ASP.NET Core 9.0 + EF Core
- **Frontend:** KnockoutJS + Bootstrap 5
- **Database:** PostgreSQL 17
