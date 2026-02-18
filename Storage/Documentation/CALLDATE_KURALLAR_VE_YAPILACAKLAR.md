# CallDate/ControlDate Kuralları ve Yapılacak İşler

**Son Güncelleme**: 2024  
**Durum**: ⚠️ Kısmen Tamamlandı - Kontrol Gerekli  
**Kaynak**: KURALLAR.md (Satır 2177-2196) + Bilgiler

---

## 📋 YAPILACAK İŞLER (ÖNCELİK SIRASI)

### 🔴 TAMAMLANDI - Dış Değerlendirmeler WHERE ControlDate Ekle
- **Dosya**: `CustomerPortalDataService.cs` (Satır 2088-2095)
- **Method**: `GetExternalEvaluationsAsync()`
- **Yapılan**: startDate/endDate filtresinde CallDate OR ControlDate kontrol etmesi eklendi
- **Status**: ✅ DONE

### 🔴 TAMAMLANDI - Excel Exports Kontrol
- **Dosya**: Excel export metodları (ReportService, CustomerPortalDataService)
- **Yapılan**: Tarih sütunları kontrol edildi
- **Status**: ✅ DONE

### 🔴 YAPILACAK #1 - GetEvaluationsAsync() Kontrol (~30 dk)
```
Dosya: CustomerPortalDataService.cs (~Satır 1500)
Kontrol:
  [ ] WHERE klozunda hangi tarih alanı kullanılıyor?
  [ ] ORDER BY'da hangi tarih alanı kullanılıyor?
  [ ] SELECT'te hangi tarih alanı dönüyor?
```

### 🔴 YAPILACAK #2 - View Dosyaları Kontrol (~30 dk)
```
Dosyalar:
  [ ] InternalEvaluations.cshtml - Tarih sütunu gösteriliyor mu?
  [ ] ExternalEvaluations.cshtml - Tarih sütunu gösteriliyor mu?
  [ ] Evaluations.cshtml - Tarih sütunu gösteriliyor mu?
```

### 🟡 YAPILACAK #3 - Frontend Dinamik Sıralama (~30 dk, OPSIYONEL)
```
Dosyalar:
  [ ] internalEvaluations.js - Tarih sütununa tıklandığında sırala
  [ ] externalEvaluations.js - Tarih sütununa tıklandığında sırala
```

### 🟡 YAPILACAK #4 - Diğer Servislerde CallDate Kuralları Kontrol
```
Dosyalar:
  [ ] EvaluationService.cs - WHERE, ORDER BY, GROUP BY
  [ ] ReportService.cs - Excel exports
  [ ] FieldWorkerService.cs - Dinleme oluşturma
  [ ] ExcelTemplateService.cs - Excel şablonları
```

---

## 📌 KURALLAR

### Kural #1: CustomerPortal Değerlendirme Tarihleri (ZORUNLU!)

⛔ **CustomerPortal'da değerlendirme tarihi olarak ASLA `CreatedAt` KULLANMA!**

**Doğru Kullanım**:
| Durum | Kullanılacak Alan |
|-------|-------------------|
| İç Değerlendirme | `e.CallDate` |
| Dış Değerlendirme | `e.CallDate ?? e.ControlDate` |
| Survey/Enneagram | `e.CreatedAt` (bunlarda CallDate/ControlDate YOK) |
| Fallback | `e.CallDate ?? e.ControlDate ?? e.CreatedAt` |

**Geçerli Yerler**:
- WHERE filtreleri
- ORDER BY sıralaması
- SELECT display alanları
- GROUP BY gruplandırması

**Neden**:
- `CreatedAt` = sistemin kaydı oluşturduğu an (müşteriyi ilgilendirmez)
- `CallDate` = dinlemenin yapıldığı tarih (müşteri bilmesi gereken)
- `ControlDate` = denetimin yapıldığı tarih (müşteri bilmesi gereken)

---

### Kural #2: Excel Exports - Tarih Alanı Seçimi (ZORUNLU!)

Excel oluştururken hangi tarih alanı kullanılacağı, **kimin performansını raporladığına bağlı**!

| Rapor Tipi | Kullanılacak Alan | Neden |
|------------|-------------------|-------|
| Kendi Personeli Performansı | **CreatedAt** | Sistem kaydı tarihi |
| Müşteri Personeli Performansı | **CallDate** | Dinleme tarihi |
| Şube/Organization Performansı | **ControlDate** | Denetim tarihi |

**Örnek - Müşteri Personeli Raporu**:
```csharp
// ❌ YANLIŞ
var evaluations = query
    .OrderByDescending(e => e.CreatedAt) // YANLIŞ!
    .Select(e => new { evaluationDate = e.CreatedAt });

// ✅ DOĞRU
var evaluations = query
    .OrderByDescending(e => e.CallDate) // DOĞRU!
    .Select(e => new { evaluationDate = e.CallDate });
```

---

### Kural #3: Dış Değerlendirmeler - ControlDate Filtreleme

**Sorun Çözüldü**: Tarih filtresi sadece CallDate'i kontrol etmişti, ControlDate olan kayıtları kaçırıyordu.

**Çözüm** (Uygulandı):
```csharp
if (startDate.HasValue)
{
    var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
    query = query.Where(e => 
        (e.CallDate.HasValue && e.CallDate.Value >= start) ||
        (e.ControlDate.HasValue && e.ControlDate.Value >= start)
    );
}

if (endDate.HasValue)
{
    var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
    query = query.Where(e => 
        (e.CallDate.HasValue && e.CallDate.Value <= end) ||
        (e.ControlDate.HasValue && e.ControlDate.Value <= end)
    );
}
```

---

## 🏗️ ARKİTEKTÜR

### CustomerPortal vs Admin Panel Ayrılığı

✅ **YAPILMIŞ**:
- CustomerPortalDataService.cs - Müşteri portalı için ayrı metotlar
- EvaluationService.cs - Admin paneli için metotlar
- CustomerPortalController - Portal endpoints
- EvaluationsApiController - Admin endpoints

✅ **ÖNEMLİ**: 
- İç ve Dış denetim puanlaması **AYNI** (formül aynı)
- CallDate/ControlDate sadece **TARIH GÖSTERİMİ** için (puanlamada rol YOKU)

---

## 🔍 KONTROL LİSTESİ

```
CallDate/ControlDate Kullanımı Kontrolü:
┌────────────────────────────────────────────┐
│ CUSTOMERPORTAL:                            │
│ [✅] GetInternalEvaluationsAsync - OK     │
│ [✅] GetExternalEvaluationsAsync - FIXED  │
│ [  ] GetEvaluationsAsync - KONTROL        │
│ [  ] Excel Exports - KONTROL              │
│ [  ] Views - Tarih sütunları              │
│ [  ] JS - Dinamik sıralama (OPSIYONEL)   │
│                                            │
│ ADMIN/REPORTS:                             │
│ [  ] ReportService exports - KONTROL      │
│ [  ] EvaluationService queries - KONTROL  │
│ [  ] FieldWorkerService - KONTROL         │
│ [  ] ExcelTemplateService - KONTROL       │
└────────────────────────────────────────────┘
```

---

## 📁 İncelenmesi Gereken Dosyalar

### CustomerPortal - Tam Kontrol
```
Backend/SecretCustomer.Services/Services/
└─ CustomerPortalDataService.cs
   ├─ GetInternalEvaluationsAsync() ✅
   ├─ GetExternalEvaluationsAsync() ✅
   ├─ GetEvaluationsAsync() ❓
   ├─ ExportAllEvaluationsToExcelAsync() ❓
   └─ ... diğer metodlar

Backend/SecretCustomer.API/Controllers/Api/
└─ CustomerPortalController.cs ✅

Backend/SecretCustomer.API/Views/CustomerPortal/
├─ InternalEvaluations.cshtml ❓
├─ ExternalEvaluations.cshtml ❓
└─ Evaluations.cshtml ❓

Backend/SecretCustomer.API/wwwroot/js/CustomerPortal/
├─ internalEvaluations.js ❓
└─ externalEvaluations.js ❓
```

### Admin/Reports - Kontrol Gerekli
```
Backend/SecretCustomer.Services/Services/
├─ ReportService.cs ⚠️
│  ├─ ExportCustomerEvaluationReportAsync()
│  ├─ ExportInternalEvaluationReportAsync()
│  ├─ ExportQuestionGroupAverageReportAsync()
│  ├─ ExportPenaltiesAsync()
│  └─ ... diğer exports
├─ EvaluationService.cs ⚠️
│  ├─ GetEvaluationsByFilterAsync()
│  ├─ GetEvaluationListAsync()
│  └─ ... diğer queries
├─ FieldWorkerService.cs ⚠️
└─ ExcelTemplateService.cs ⚠️

Backend/SecretCustomer.API/Controllers/Api/
├─ EvaluationsApiController.cs ⚠️
└─ ReportsApiController.cs ⚠️
```

---

## 📊 Excel Exports Kontrol Tablosu

| Export Metodu | Dosya | Kiminle İlgili? | Beklenen Tarih Alanı | Status |
|---------------|-------|-----------------|----------------------|--------|
| ExportCustomerEvaluationReportAsync | ReportService | Müşteri Personeli | CallDate | ❓ |
| ExportInternalEvaluationReportAsync | ReportService | İç Denetim | ? | ❓ |
| ExportQuestionGroupAverageReportAsync | ReportService | ? | ? | ❓ |
| ExportPenaltiesAsync | ReportService | ? | ? | ❓ |
| ExportAllEvaluationsToExcelAsync | CustomerPortalDataService | Müşteri | CallDate/ControlDate | ✅ |
| ExportScoreDistributionEvaluationsAsync | CustomerPortalDataService | Müşteri | ? | ❓ |

---

## 🎯 Başlama Stratejisi

**Adım 1**: YAPILACAK #1'i tamamla (GetEvaluationsAsync) - 30 dk
**Adım 2**: YAPILACAK #2'yi tamamla (Views) - 30 dk
**Adım 3**: YAPILACAK #4'ü kontrol et (Diğer Servisler) - 2-3 saat
**Adım 4**: YAPILACAK #3'ü yap (Frontend, opsiyonel) - 30 dk

---

## 📝 NOTLAR

- ✅ CustomerPortal ayrılığı: YAPILMIŞ
- ✅ Dış Değerlendirmeler ControlDate: YAPILMIŞ
- ⚠️ Excel Exports: Kısmen kontrol edildi, tam uygunluk YOK
- ⚠️ Diğer Servisler: KONTROL YAPILMADI
- ⚠️ Frontend: KONTROL YAPILMADI

---

**Tamamlandığında**: Bu dosya güncellenerek tüm ✅ işaretlenmiş olacak.
