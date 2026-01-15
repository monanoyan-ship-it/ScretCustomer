# SecretCustomer Project - Claude Instructions

## ⛔ EN ÖNEMLİ NOT: YALAN SÖYLEMEK YOK ⛔

- **"Bilmiyorum, kontrol edeyim"** de
- **"Emin değilim, bakayım"** de
- Kısmi bilgiyle kesin konuşma
- **Hızlı değil, DOĞRU iş yap**
- Yalan = zaman hırsızlığı (kullanıcı test eder, hata bulur, geri döner, düzeltirsin = 3x zaman)
- "Yaptım" demeden önce MUTLAKA kontrol et (grep, glob)
- Bir şeyi "her yerde düzelttim" diyorsan, grep ile KONTROL ET

## ⚠️ UNUTKANLIK SORUNU

Ben konuştuğum konuyu unutan bir yapay zekayım. Bunu farkeden kullanıcım promptları hazırlamam için pattern ve kurallar hazırladı - ona rağmen unutuyorum.

---

## ⚠️ SESSION BAŞINDA YAPILACAKLAR

1. **BU DOSYAYI BAŞTAN SONA OKU** - Her yeni session'da bu kuralları tekrar oku
2. **Commit kurallarını EZBERLE** - Aşağıdaki commit kuralları bölümünü oku

## ZORUNLU: Her İşe Başlamadan Önce

**YENİ BİR ÖZELLİK VEYA MODÜL EKLEMEDEN ÖNCE MUTLAKA `DEVELOPMENT_PATTERNS.md` DOSYASINI OKU!**

```
Read: DEVELOPMENT_PATTERNS.md
```

Bu dosya projenin standart pattern'lerini içerir:
- View/JavaScript yapısı (SPA Modal Pattern)
- KnockoutJS binding kuralları
- API Controller yapısı
- Doğru ve yanlış örnekler

## Proje Özeti

- **Proje:** Gizli Müşteri Değerlendirme Sistemi (Mystery Shopper)
- **Backend:** ASP.NET Core 9.0 + Entity Framework Core
- **Frontend:** KnockoutJS + Bootstrap 5
- **Database:** PostgreSQL
- **Pattern:** SPA-like with server-rendered views + AJAX

## Kritik Kurallar

1. **Tek Index.cshtml** - Her modül için sadece bir Index.cshtml olmalı
2. **Modal ile CRUD** - Create/Edit/Detail işlemleri ayrı sayfa değil, modal ile yapılmalı
3. **Spesifik Binding** - `ko.applyBindings(vm, document.getElementById('app-id'))` kullan
4. **Ayrı Sayfa YOK** - Create.cshtml, Edit.cshtml, Detail.cshtml OLMAMALI
5. **YALAN SÖYLEME** - "Kaldırıldı", "Yok" demeden önce MUTLAKA kontrol et (Glob, Grep kullan)
6. **Varsayımda bulunma** - Entity, dosya veya özellik hakkında emin olmadan konuşma
7. **JS Localization Pattern** - T() kullanan her JS dosyasında:
   ```javascript
   var TRANSLATION_KEYS = ['Key1', 'Key2', ...];

   Localization.loadKeys(TRANSLATION_KEYS).then(function() {
       ko.applyBindings(new ViewModel(), document.getElementById('app'));
   });
   ```
   - Layout'da `localization.js` dahil olmalı
   - `ko.applyBindings` MUTLAKA `Localization.loadKeys().then()` içinde olmalı

## Entity Yapısı Notları

- **Section**: ~~Entity olarak var ama kullanılmıyor~~ **SİLİNDİ** (Ocak 2026). Questions artık direkt Checklist'e bağlı.
- **GroupName**: Question entity'sinde var, sadece RAPORLAMA için gruplama amaçlı. UI'da gruplama yapılmamalı.
- Section referansları kaldırıldı, `.ThenInclude(c => c.Questions)` kullanılmalı.

## Commit Kuralları

⛔ **KULLANICI COMMIT DEMEDİKÇE ASLA COMMIT YAPMA!** ⛔

- "commit et", "bitince commit et", "commit yap" = geçerli komut, UYGULA
- Kullanıcı hiçbir şey demediyse = COMMIT YAPMA
- Her yeni iş için AYRI commit izni gerekir (önceki izin yeni işler için geçerli değil)
- Kullanıcı onayı olmadan push yapılmaz
- Şüphen varsa SOR: "Commit edeyim mi?"

## Proje Yapısı

```
Backend/
  SecretCustomer.API/           # Web API + MVC Controllers + Views
  SecretCustomer.Core/          # Entities, DTOs, Interfaces
  SecretCustomer.Data/          # EF Core, Repositories, Migrations
  SecretCustomer.Services/      # Business Logic
```

## Önemli Dosyalar

**NOT:** Dokümantasyon dosyaları `Storage/` klasöründe (gitignore - müşteriye gitmez)

- `Storage/Documentation/KALDIGIMIZ_YER.md` - **"Nerede kaldık?" diye sorulursa bu dosyayı oku!**
- `Storage/Documentation/AIReports.md` - **"AI ile ilgili ne yapacaktık?" diye sorulursa bu dosyayı oku!**
- `Storage/Documentation/mp4analizi.xml` - Müşteri geribidirim analizi ve özellik takibi
- `Storage/Documentation/DEVELOPMENT_PATTERNS.md` - Kod standartları ve pattern'ler
- `Storage/docs/` - Teknik dokümantasyon (proje kurulumu, mimari vb.)
- `Storage/ScreenShots/` - Test dosyaları, Excel/CSV import örnekleri

## Production Publish

**ÖNEMLİ:** Publish çıkarken mutlaka eski dosyaları temizle! (hem `publish` hem `obj` klasörü)

```bash
# Önce eski klasörleri sil (iç içe publish sorununu önler):
rmdir /s /q Backend\SecretCustomer.API\publish
rmdir /s /q Backend\SecretCustomer.API\obj

# Sonra publish çıkar:
dotnet publish Backend\SecretCustomer.API\SecretCustomer.API.csproj -c Release -o Backend\SecretCustomer.API\publish --self-contained false
```

**NOT:** `publish/` klasörü `.gitignore`'a eklenmiştir. Git'e commit edilmemelidir.

**UYARI:** `obj` klasöründe eski publish cache dosyaları kalırsa, iç içe `publish/publish/publish/...` klasörleri oluşabilir. Bu yüzden publish öncesi obj klasörünü silmek önemlidir.

**ScreenShots Klasörü:** Test dosyaları artık `Storage/ScreenShots/` klasöründe (gitignore - müşteriye gitmez).

## Düzeltilmiş Modüller

Bu modüller artık doğru SPA Modal Pattern'i kullanıyor:
- ✅ Calls (tek Index.cshtml)
- ✅ Trainings (tek Index.cshtml)
- ✅ Meetings (tek Index.cshtml)
- ✅ Approvals (tek Index.cshtml)

