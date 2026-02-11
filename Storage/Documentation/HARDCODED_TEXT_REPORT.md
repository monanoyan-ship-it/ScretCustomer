# Hardcoded Türkçe Metin Raporu

**Tarih:** 2026-02-11
**Toplam bulgu:** 2761 satır (filtreleme öncesi)
**Gerçek aksiyon gereken:** ~1500-1800 (TypeDefinitions fallback, migration, yorum çıkınca)

## Özet

| Tip | Bulgu | Dosya | Not |
|-----|-------|-------|-----|
| .cshtml | 484 | 45 | HTML text, data-bind, placeholder, title attr |
| .js | 710 | 62 | toastr, status map, tablo başlık, filtre label |
| .cs | 1567 | 97 | API mesajları, Excel başlıkları, validation |

## Öncelik Sırası

### P1: Kullanıcıya Direkt Görünen (Views + JS)

**CSHTML - En Çok Bulgu:**
- `Settings/Smtp.cshtml` (57) - SMTP ayarları sayfası, label+placeholder
- `Customers/Organizations.cshtml` (45) - Organizasyon yönetimi
- `EmailTemplates/Index.cshtml` (27) - E-posta şablonları
- `CustomerPortal/PersonnelReportCard.cshtml` (27) - Personel karnesi
- `Listenings/Index.cshtml` (22) - Dinlemeler sayfası
- `Reports/AIReport.cshtml` (20) - AI rapor
- `Projects/Index.cshtml` (19) - Projeler

**JS - En Çok Bulgu:**
- `Projects/Index.js` (71) - Proje yönetimi, status map'ler
- `Customers/organizations.js` (38) - Organizasyon işlemleri
- `Listenings/index.js` (33) - Dinleme filtreleri, kolonlar
- `Visits/index.js` (28) - Ziyaret raporları
- `CustomerPortal/suggestions.js` (27) - Öneriler
- `CustomerPortal/personnelReportCard.js` (26) - Personel karnesi
- `CustomerPortal/internalEvaluations.js` (26) - İç değerlendirmeler

### P2: API/Backend Mesajları

**CS - En Çok Bulgu:**
- `ReportService.cs` (218) - Excel başlıkları, sheet adları
- `CustomerPortalController.cs` (168) - API response mesajları
- `ReportsApiController.cs` (94) - Rapor API mesajları
- `SurveyApiController.cs` (76) - Anket API mesajları
- `AssignmentService.cs` (47) - Atama servisi
- `TrainingVideosApiController.cs` (46) - Eğitim videoları
- `TrainingQuizApiController.cs` (40) - Quiz API

### P3: Düşük Öncelik / False Positive

- `TypeDefinitions.cs` - Zaten localization key'i var, fallback description
- Migration dosyaları - Veritabanı migration'ları
- Validator attribute'ları - [Required(ErrorMessage="...")] pattern'leri

## Hardcoded Türkçe Pattern'leri

### 1. data-bind içi inline text (CSHTML)
```html
<span data-bind="text: totalCount() + ' kayıt'"></span>
<span data-bind="text: status === 'Open' ? 'Açık' : 'Kapalı'"></span>
```
**Çözüm:** JS tarafında T() ile çevirip observable'a ata

### 2. HTML düz metin (CSHTML)
```html
<span>Online Anket Ayarları</span>
<small>Bağımsız Operatörler</small>
```
**Çözüm:** `@Html.T("Key", "Fallback")` ile sar

### 3. placeholder / title attribute (CSHTML)
```html
<input placeholder="Başlangıç tarihi..." />
<button title="Organizasyondan Çıkar">
```
**Çözüm:** `placeholder="@Html.T("Key", "Fallback")"` ile sar

### 4. JS string literal (JS)
```js
toastr.error('Hata oluştu');
{ name: 'Tamamlandı', value: 'Completed' }
columns: [{ title: 'Dönem' }]
```
**Çözüm:** `T('Key', 'Fallback')` ile sar

### 5. C# API mesajları (CS)
```csharp
return BadRequest("Değerlendirme bulunamadı");
worksheet.Cell(1, 1).Value = "Proje Adı";
```
**Çözüm:** `T("Key", "Fallback")` ile sar
