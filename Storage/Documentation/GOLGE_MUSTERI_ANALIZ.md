# Gölge Müşteri Modülü - Analiz & Uygulama Durumu

## Kaynak: `subat2026golgemusteri.xlsx`

---

## Modül Özeti

Müşterinin (işi veren firma) istediği dış firmalara telefon araması yapılmasını yöneten bağımsız modül. Mevcut Evaluation/Assignment yapısından tamamen ayrı. Arama yapan personel puanlama/değerlendirme yapmaz - sadece tarih/saat/not kaydeder.

### Temel Kavramlar
- **Hedef Firma**: Aranacak dış kurum (Bilyoner, Nesine vb.) - bizim müşterimiz DEĞİL
- **Soru**: Hedef firmaya sorulacak soru + beklenen cevap + aranma sayısı + kuponlu flag
- **Dönem**: Aylık plan (Mart 2026 gibi) - mevcut dönem tablosundan BAĞIMSIZ
- **Kupon**: Dönem bazlı kupon kodları listesi (kuponlu sorular için)
- **Atama**: Personele dağıtılmış tekil arama görevi

---

## Veritabanı Yapısı (TAMAMLANDI)

### Entity'ler ve Tablolar

| Entity | Tablo | FK'lar | Önemli Alanlar |
|--------|-------|--------|----------------|
| `GmHedefFirma` | GmHedefFirmalar | CustomerId→Customer | FirmaAdi, TelefonNo, Aciklama, IsActive |
| `GmSoru` | GmSorular | CustomerId, GmHedefFirmaId | SoruMetni, BeklenenCevap, AranmaSayisi, IsKuponlu, SiraNo, IsActive |
| `GmDonem` | GmDonemler | CustomerId, OlusturanUserId→User | Ad, BaslangicTarihi, BitisTarihi, DurumId |
| `GmDonemKupon` | GmDonemKuponlar | GmDonemId | KuponKodu, IsUsed |
| `GmDonemPersonel` | GmDonemPersoneller | GmDonemId, UserId→User | (sadece FK'lar) |
| `GmDonemSoru` | GmDonemSorular | GmDonemId, GmSoruId | AranmaSayisi (dönem bazlı override) |
| `GmAtama` | GmAtamalar | GmDonemId, GmDonemSoruId, UserId | PlanTarihi, GerceklesmeTarihi, AramaSaati, Not, KuponKodu, DurumId |

### Enum'lar (TypeDefinitions.cs)
- **GmDonemDurumlari**: Taslak(1), Aktif(2), Tamamlandi(3)
- **GmAtamaDurumlari**: Beklemede(1), Tamamlandi(2)

### Migration
- `AddGolgeMusteriTables` - 7 tablo, tüm FK'lar ve index'ler

---

## Dosya Yapısı

### Entity'ler (`Backend/SecretCustomer.Core/Entities/`)
- `GmHedefFirma.cs`, `GmSoru.cs`, `GmDonem.cs`
- `GmDonemKupon.cs`, `GmDonemPersonel.cs`, `GmDonemSoru.cs`
- `GmAtama.cs`

### EF Configurations (`Backend/SecretCustomer.Data/Configurations/`)
- `GmHedefFirmaConfiguration.cs`, `GmSoruConfiguration.cs`, `GmDonemConfiguration.cs`
- `GmDonemKuponConfiguration.cs`, `GmDonemPersonelConfiguration.cs`, `GmDonemSoruConfiguration.cs`
- `GmAtamaConfiguration.cs`

### DTO'lar (`Backend/SecretCustomer.Core/DTOs/GolgeMusteri/`)
- `GmHedefFirmaDto.cs` → GmHedefFirmaDto, CreateGmHedefFirmaDto, UpdateGmHedefFirmaDto
- `GmSoruDto.cs` → GmSoruDto, CreateGmSoruDto, UpdateGmSoruDto
- `GmDonemDto.cs` → GmDonemDto, GmDonemDetailDto, CreateGmDonemDto, UpdateGmDonemDto, GmDonemPersonelDto, GmDonemSoruDto, GmDonemKuponDto
- `GmAtamaDto.cs` → GmAtamaDto, CompleteGmAtamaDto

### Service (`Backend/SecretCustomer.Services/Services/`)
- `GmService.cs` → IGmService + GmService (tek servis, modül bağımsız)

### Controller'lar (`Backend/SecretCustomer.API/Controllers/`)
- `Api/GmApiController.cs` → Admin CRUD + dönem yönetimi
- `Api/GmAramalarimApiController.cs` → Kullanıcı aramaları
- `GolgeMusteriController.cs` → MVC view rendering

### View'lar (`Backend/SecretCustomer.API/Views/GolgeMusteri/`)
- `HedefFirmalar.cshtml`, `Sorular.cshtml`, `Donemler.cshtml`, `Takip.cshtml`, `Aramalarim.cshtml`

### JS (`Backend/SecretCustomer.API/wwwroot/js/GolgeMusteri/`)
- `hedefFirmalar.js`, `sorular.js`, `donemler.js`, `takip.js`, `aramalarim.js`

---

## Sayfalar

### Admin Sayfaları
| Sayfa | Route | Açıklama |
|-------|-------|----------|
| Hedef Firmalar | /GolgeMusteri/HedefFirmalar | Firma CRUD, müşteri filtresi |
| Sorular | /GolgeMusteri/Sorular | Soru CRUD, firma filtresi (cascading) |
| Dönemler | /GolgeMusteri/Donemler | Dönem yönetimi, 4 tab (genel/personel/soru/kupon) |
| Takip | /GolgeMusteri/Takip | İlerleme takibi, filtreler |

### Kullanıcı Sayfası
| Sayfa | Route | Açıklama |
|-------|-------|----------|
| Aramalarım | /GolgeMusteri/Aramalarim | Atanmış aramalar, tamamlama formu |

### Sidebar Menü
```
GÖLGE MÜŞTERİ (bi-telephone-outbound)   ← Admin accordion
  ├── Hedef Firmalar (bi-building)
  ├── Sorular (bi-question-circle)
  ├── Dönemler (bi-calendar3)
  └── Takip (bi-clipboard-data)

Aramalarım (bi-telephone)               ← Tüm login kullanıcılar
```

---

## Adil Dağıtım Algoritması
1. Dönemin sorularını ve aranma sayılarını al
2. Dönemin personellerini al
3. Her soru için AranmaSayisi kadar GmAtama üret
4. Personellere round-robin dağıt
5. PlanTarihi'ni dönem içindeki iş günlerine yay

---

## Uygulama Durumu

| Faz | Açıklama | Durum |
|-----|----------|-------|
| 1 | Entity + Migration + DbContext | ✅ TAMAMLANDI |
| 2 | DTO'lar | ✅ TAMAMLANDI |
| 3 | Service katmanı (IGmService + GmService) | ✅ TAMAMLANDI |
| 4 | Controller'lar (API + MVC) | ✅ TAMAMLANDI |
| 5 | View + JS dosyaları (5 sayfa) | ✅ TAMAMLANDI |
| 6 | Sidebar menü | ✅ TAMAMLANDI |
| 7 | Localization | ✅ TAMAMLANDI |

---

## Excel'den Çıkan İstatistikler (Referans)
- **542 arama** planlanmış (Şubat 2026, 16 iş günü)
- **13 firma**, her birinin sabit telefon numarası var
- **10 personel** + 1 "Kuponlu" kategorisi
- **43 unique soru** - firma bazlı soru havuzu
- Kişi başı günde **2-3 arama** (adil dağıtılmış)
