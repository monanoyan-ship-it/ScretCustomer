# Claude Code Proje Kuralları

> **Her oturumda bu dosyayı oku ve kurallara uy.**

## ClaudeManager Entegrasyonu (ZORUNLU!)

- **Proje ID:** 12
- **Proje adı:** ScretCustomer
- **cwd:** `c:/Users/Ahmet/source/repos/monanoyan-ship-it/ScretCustomer` (forward slash, `$(pwd)` KULLANMA)

**Her oturum başında** guide'ı oku ve dönen kuralları uygula:

```bash
curl -s "http://127.0.0.1:41847/api/guide?cwd=c:/Users/Ahmet/source/repos/monanoyan-ship-it/ScretCustomer"
```

Bu API tüm pattern'leri, roadmap durumunu ve kullanılabilir endpoint'leri tek seferde döner. **Tüm kurallara uy!**

### 1. Pattern'ler (Kurallar / Hatalar / Tercihler)

Pattern tipleri: `rule`, `mistake`, `preference`

```bash
# Oku
curl -s http://127.0.0.1:41847/api/projects/12/patterns

# Yeni kaydet
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":12,"type":"rule|mistake|preference","title":"BASLIK","description":"ACIKLAMA"}'

# Güncelle
curl -X PUT http://127.0.0.1:41847/api/patterns/PATTERN_ID -H "Content-Type: application/json" \
  -d '{"title":"YENI_BASLIK","description":"YENI_ACIKLAMA"}'

# Sil
curl -X DELETE http://127.0.0.1:41847/api/patterns/PATTERN_ID
```

**Ne zaman kaydet:**
- Kullanıcı bir hata yaptığını söylerse → `mistake`
- Kullanıcı bir tercih belirtirse → `preference`
- Tekrarlanan bir pattern fark edersen → `rule`

### 2. Yol Haritası / Planlama

Büyük bir iş planlandığında (analiz, refactoring, yeni modül vb.) **Roadmap API** ile takip et:

```bash
# Oku (her oturum başında kontrol et)
curl -s http://127.0.0.1:41847/api/projects/12/roadmap
curl -s http://127.0.0.1:41847/api/projects/12/roadmap/stats

# Yeni faz
curl -X POST http://127.0.0.1:41847/api/projects/12/phases \
  -H "Content-Type: application/json" -d '{"phase_no":"1","title":"FAZ ADI"}'

# Faza görev ekle
curl -X POST http://127.0.0.1:41847/api/phases/FAZ_ID/tasks \
  -H "Content-Type: application/json" -d '{"task_no":"1.1","title":"GOREV","detail":"DETAY","risks":"RISKLER"}'

# Görev durumu güncelle (başlarken/bitirince)
curl -X PUT http://127.0.0.1:41847/api/tasks/GOREV_ID \
  -H "Content-Type: application/json" -d '{"status":"in_progress|completed|cancelled"}'

# XML'den toplu import (büyük planlar için)
curl -X POST http://127.0.0.1:41847/api/projects/12/roadmap/import \
  -H "Content-Type: application/json" -d '{"xml":"<Faz no=\"1\" ad=\"Faz Adi\" durum=\"planned\"><Gorev no=\"1.1\" durum=\"planned\"><Ad>Gorev adi</Ad><Detay>Aciklama</Detay><Riskler>Risk</Riskler></Gorev></Faz>"}'
```

**Planlama kuralları:**
- Yeni iş başlamadan önce roadmap'i oku, açık görev var mı kontrol et
- Görev başlarken `in_progress`, bitirince `completed` yap
- Büyük refactoring/özellik fazlara bölünsün (ör: Aşama 1, Aşama 2)
- Her görevin `detail` ve `risks` alanlarını doldur

### 3. Günlük (Journal)

Günlük nitelikli bilgiler (kredi durumu, domain, vize, kararlar vb.) için:

```bash
# Oku
curl -s http://127.0.0.1:41847/api/projects/12/journal

# Yeni giriş
curl -X POST http://127.0.0.1:41847/api/projects/12/journal -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"genel|teknik|karar|arastirma"}'
```

### 4. Notlar (Notes)

Hesap bilgileri, API key'ler, şifreler, konfigürasyonlar gibi kalıcı bilgiler:

```bash
# Oku
curl -s http://127.0.0.1:41847/api/projects/12/notes

# Yeni not
curl -X POST http://127.0.0.1:41847/api/projects/12/notes -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'
```

**Kural:** Yeni bir hesap/servis oluşturulursa bilgileri **hemen** Notes'a yaz — sonra unutulur.

### 5. Arama ve Analitik

```bash
# Geçmiş prompt'larda ara
curl -s "http://127.0.0.1:41847/api/search?q=ARAMA_TERIMI&project=12"

# Analitik
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
