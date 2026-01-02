# SecretCustomer Project - Claude Instructions

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

## Proje Yapısı

```
Backend/
  SecretCustomer.API/           # Web API + MVC Controllers + Views
  SecretCustomer.Core/          # Entities, DTOs, Interfaces
  SecretCustomer.Data/          # EF Core, Repositories, Migrations
  SecretCustomer.Services/      # Business Logic
```

## Önemli Dosyalar

- `KALDIGIMIZ_YER.md` - **"Nerede kaldık?" diye sorulursa bu dosyayı oku!**
- `mp4analizi.xml` - Müşteri geribidirim analizi ve özellik takibi
- `DEVELOPMENT_PATTERNS.md` - Kod standartları ve pattern'ler
- `is kapsam.docx` - Proje kapsamı

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

**ScreenShots Klasörü:** `wwwroot/ScreenShots/` klasörü publish'e dahil EDİLMEZ (csproj'da exclude edildi). Bu klasörde mp4/png gibi geliştirme dosyaları bulunur.

## Düzeltilmiş Modüller

Bu modüller artık doğru SPA Modal Pattern'i kullanıyor:
- ✅ Calls (tek Index.cshtml)
- ✅ Trainings (tek Index.cshtml)
- ✅ Meetings (tek Index.cshtml)
- ✅ Approvals (tek Index.cshtml)
