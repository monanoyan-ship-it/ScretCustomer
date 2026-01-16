# Claude Code Proje Kuralları

> **Bu dosyanın yolu:** `C:\Users\ahmet\source\repos\ScretCustomer\CLAUDE.md`
> **Her oturumda bu dosyayı oku ve kurallara uy.**

## Git Kuralları (KESİN UYULMASI GEREKEN)

1. **Git revert/reset YASAK** - Kullanıcı net ve açık bir şekilde emir vermeden `git revert`, `git reset`, `git checkout --` gibi geri alma komutları ASLA çalıştırılmaz.

2. **Port belirtme** - Uygulama kendi portunda çalışıyor, `dotnet run` komutunda port belirtme (5000 vs.).

## Kod Değişikliklerinde Kontrol Listesi

Her entity/özellik eklerken şunları MUTLAKA kontrol et:
- [ ] Entity dosyası
- [ ] DTO dosyaları (ChecklistDto, UpdateChecklistDto, vb.)
- [ ] Service dosyaları (mapping'ler)
- [ ] Controller dosyaları (API response)
- [ ] JavaScript dosyaları (observable'lar)
- [ ] View dosyaları (UI binding)
- [ ] Migration

## Veritabanı

- PostgreSQL kullanılıyor
- Migration'lar `SecretCustomer.Data` projesinde
- Migration komutu: `cd Backend/SecretCustomer.Data && dotnet ef migrations add MigrationName --startup-project ../SecretCustomer.API`

## Proje Yapısı

- Backend: ASP.NET Core
- Frontend: Knockout.js
- Veritabanı: PostgreSQL
