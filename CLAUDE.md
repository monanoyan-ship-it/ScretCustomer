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

## Zorunlu Okunacak Dosyalar

Her oturumda ve yeni özellik eklemeden önce bu dosyaları oku:
- `Storage/Documentation/KURALLAR.md` - Development pattern'leri ve standartlar
- `Storage/Documentation/CLAUDE.md` - Detaylı proje talimatları

## Claude Memory Dosyaları (Oturum Başında Kontrol Et)

Önceki oturumlarda yapılan işler ve devam eden görevler için bu dosyaları oku:
- `~/.claude/projects/.../memory/MEMORY.md` - Proje hafızası, öğrenilen dersler, tamamlanan işler
- `~/.claude/projects/.../memory/PDF_EXPORT_STATUS.md` - PDF export implementasyon durumu
- `~/.claude/projects/.../memory/DAILY_REPORT_ANALYSIS.md` - Günlük dinleme raporu spec (varsa)

> **Not:** Bu dosyalar otomatik olarak system prompt'a yüklenir. Yarım kalan işler veya "PC yeniden başlatılacak" gibi durumlar burada not edilir.

## Proje Yapısı

- Backend: ASP.NET Core
- Frontend: Knockout.js
- Veritabanı: PostgreSQL
