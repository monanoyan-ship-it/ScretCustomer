# Kaldığımız Yer - 15 Aralık 2025

## Son Commit
- **Hash:** 7390e3f
- **Başlık:** Almanca çeviri + Kullanıcı dil tercihi + Production seed

---

## Yapılanlar

### 1. Almanca Lokalizasyon (Tamamlandı)
- `resources.de.xml` tamamen Almanca'ya çevrildi (~5000 satır)
- Tüm bölümler: Header, Common, Layout, Permission, Organization, Delegation, Profile, Account, Login, Menu, VisitDetails, Dashboard, Customer, FieldWorker, Personnel, Project, Branch, Checklist, Evaluations, Meetings, Calls, Trainings, Reports, Users, Pagination, Validation, Messages, Settings, Languages, CustomerPortal, Approvals, Notifications, Excel Templates, Form, API Controller mesajları

### 2. Kullanıcı Dil Tercihi Sistemi
- `User.PreferredLanguageId` eklendi
- `CustomerPersonnel.PreferredLanguageId` eklendi
- Dil değişikliği artık veritabanına kaydediliyor
- Login sonrası kayıtlı dil tercihi cookie'ye uygulanıyor
- **Sonuç:** Farklı browser'da login olunca aynı dil geliyor

### 3. Production Seed (Temiz Kurulum)
- `SeedData.InitializeProductionAsync()` eklendi
- IIS'de `ASPNETCORE_ENVIRONMENT=Production` ile çalışır
- Oluşturur:
  - Sadece admin kullanıcı (`admin / Admin@123`)
  - 4 dil (TR, EN, ES, DE)
  - XML'den çeviriler import
  - Temel ayarlar ve yetkiler

### 4. TopBar Dil Seçici
- `isDefault` → `isCurrent` olarak değiştirildi
- Varsayılan dil işareti kaldırıldı (sadece ayarlarda görünür)
- Sadece şu an seçili dil işaretli

---

## Migration
- `AddUserLanguagePreference` migration'ı oluşturuldu
- User ve CustomerPersonnel tablolarına PreferredLanguageId eklendi

---

## IIS Kurulumu İçin
1. Yeni publish al
2. IIS Application Pool → Advanced Settings → Environment Variables:
   ```
   ASPNETCORE_ENVIRONMENT = Production
   ```
3. Veya `appsettings.Production.json` ekle:
   ```json
   {
     "UseProductionSeed": true
   }
   ```
4. Uygulama başladığında otomatik:
   - DB migration çalışır
   - Admin + diller + ayarlar oluşturulur

---

## Dosya Konumları
- Almanca çeviriler: `Backend/SecretCustomer.API/App_Data/Localization/resources.de.xml`
- Seed Data: `Backend/SecretCustomer.Data/SeedData.cs`
- Localization Service: `Backend/SecretCustomer.Services/Services/LocalizationService.cs`
- User Entity: `Backend/SecretCustomer.Core/Entities/User.cs`
