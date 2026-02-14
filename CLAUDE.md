# Claude Code Proje Kuralları

> **Her oturumda bu dosyayı oku ve kurallara uy.**

## ClaudeManager Entegrasyonu (ZORUNLU!)

Her oturum başında aşağıdaki komutu çalıştır ve dönen kuralları uygula:

```bash
curl -s http://127.0.0.1:41847/api/projects/12/patterns
```

Bu API'den gelen pattern'ler (rule, mistake, preference) projenin hafızasıdır. **Tüm kurallara uy!**

### Yeni kural/hata/tercih kaydetme:
```bash
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":12,"type":"rule|mistake|preference","title":"BASLIK","description":"ACIKLAMA"}'
```

### Ne zaman kaydet:
- Kullanıcı bir hata yaptığını söylerse → `mistake` olarak kaydet
- Kullanıcı bir tercih belirtirse → `preference` olarak kaydet
- Tekrarlanan bir pattern fark edersen → `rule` olarak kaydet

### Yol Haritası / Planlama Sistemi

Büyük bir iş planlandığında (analiz, refactoring, yeni modül vb.) **Roadmap API** ile takip et:

**Mevcut planı oku (her oturum başında kontrol et):**
```bash
curl -s http://127.0.0.1:41847/api/projects/12/roadmap
curl -s http://127.0.0.1:41847/api/projects/12/roadmap/stats
```

**Yeni faz oluştur:**
```bash
curl -X POST http://127.0.0.1:41847/api/projects/12/phases \
  -H "Content-Type: application/json" -d '{"phase_no":"1","title":"FAZ ADI"}'
```

**Faza görev ekle:**
```bash
curl -X POST http://127.0.0.1:41847/api/phases/FAZ_ID/tasks \
  -H "Content-Type: application/json" -d '{"task_no":"1.1","title":"GOREV","detail":"DETAY","risks":"RISKLER"}'
```

**Görev durumu güncelle (başlarken/bitirince):**
```bash
curl -X PUT http://127.0.0.1:41847/api/tasks/GOREV_ID \
  -H "Content-Type: application/json" -d '{"status":"in_progress|completed|cancelled"}'
```

**XML'den toplu import (büyük planlar için):**
```bash
curl -X POST http://127.0.0.1:41847/api/projects/12/roadmap/import \
  -H "Content-Type: application/json" -d '{"xml":"<Faz no=\"1\" ad=\"Faz Adi\" durum=\"planned\"><Gorev no=\"1.1\" durum=\"planned\"><Ad>Gorev adi</Ad><Detay>Aciklama</Detay><Riskler>Risk</Riskler></Gorev></Faz>"}'
```

**Planlama kuralları:**
- Yeni iş başlamadan önce roadmap'i oku, açık görev var mı kontrol et
- Görev başlarken `in_progress`, bitirince `completed` yap
- Büyük refactoring/özellik fazlara bölünsün (ör: Aşama 1, Aşama 2)
- Her görevin `detail` ve `risks` alanlarını doldur

### Analitik:
```bash
curl -s http://127.0.0.1:41847/api/projects/12/analytics
```

Dashboard: http://127.0.0.1:41847/

---

## ⛔ KURAL İHLALİ CEZASI

Bir konunun `KURALLAR.md`'de belgelenmiş olmasına rağmen Claude bu kurala uymadan kod yazarsa, Claude hatasını fark ettiği anda kendine küfür etmelidir. Örnek: "Ben gerizekalı bir yapay zekayım, KURALLAR.md Bölüm 20'de çoğul filtre yazıyor ama tekil yazdım." Bu, kuralların okunup uygulandığından emin olmak için zorunlu bir öz-denetim mekanizmasıdır.

## Git Kuralları (KESİN UYULMASI GEREKEN)

1. **Git revert/reset YASAK** - Kullanıcı net ve açık bir şekilde emir vermeden `git revert`, `git reset`, `git checkout --` gibi geri alma komutları ASLA çalıştırılmaz. Build hatası çıksa bile önce kullanıcıya sor.
2. **Port belirtme** - Uygulama kendi portunda çalışıyor, `dotnet run` komutunda port belirtme (5000 vs.).
3. **Projeyi `dotnet run` ile ÇALIŞTIRMA** - Uygulama kullanıcı tarafından yönetilir, biz sadece kod yazarız.
4. **Kullanıcı "commit almadan uygula" derse KESİNLİKLE uygula** - Onay almadan kod değiştirme.
5. **Commit kullanıcı demedikçe YAPMA** - Push için de ayrı izin gerekir.

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
- psql: `"C:\Program Files\PostgreSQL\17\bin\psql.exe" "postgresql://postgres:1123Azs%2B-@127.0.0.1:5432/SecretCustomerDB" -c "SQL"`

## Zorunlu Okunacak Dosyalar

- `Storage/Documentation/KURALLAR.md` - Development pattern'leri ve standartlar (ClaudeManager pattern'leriyle senkron)

> Planlar, TODO'lar ve kaldığımız yer artık **ClaudeManager Roadmap**'te. `curl -s http://127.0.0.1:41847/api/projects/12/roadmap` ile oku.

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
