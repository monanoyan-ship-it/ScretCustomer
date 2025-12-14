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

- `mp4analizi.xml` - Müşteri geribidirim analizi ve özellik takibi
- `DEVELOPMENT_PATTERNS.md` - Kod standartları ve pattern'ler
- `is kapsam.docx` - Proje kapsamı

## Düzeltilmiş Modüller

Bu modüller artık doğru SPA Modal Pattern'i kullanıyor:
- ✅ Calls (tek Index.cshtml)
- ✅ Trainings (tek Index.cshtml)
- ✅ Meetings (tek Index.cshtml)
- ✅ Approvals (tek Index.cshtml)
