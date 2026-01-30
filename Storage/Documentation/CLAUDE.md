# SecretCustomer - Claude Talimatları

## ⛔ BU DOSYAYI OKUDUYSAN BUNLARI DA OKU:

| Dosya | Ne Zaman Oku |
|-------|--------------|
| `Storage/Documentation/KURALLAR.md` | **HER ZAMAN** - Tüm kod standartları ve pattern'ler |
| `Storage/Documentation/KALDIGIMIZ_YER.md` | "Nerede kaldık?" dendiğinde |
| `Storage/Documentation/AIReports.md` | "AI ile ilgili ne yapacaktık?" dendiğinde |

---

## TEMEL KURALLAR (EZBERLE!)

### 1. Yalan Söyleme
- **"Bilmiyorum, kontrol edeyim"** de
- "Yaptım" demeden önce MUTLAKA kontrol et (grep, glob)
- Varsayımda bulunma, emin değilsen SOR

### 2. Commit Kuralları
- ⛔ **Kullanıcı "commit et" demedikçe ASLA commit yapma**
- Her yeni iş için AYRI commit izni gerekir
- Push için de ayrı izin gerekir

### 3. "Nerede Kaldığımızı Kaydet" Kuralı
- **SADECE `Storage/Documentation/KALDIGIMIZ_YER.md` dosyasına yaz**
- Plan dosyalarına (`~/.claude/plans/`) YAZMA
- Kaydetmeden önce kullanıcıya ne yazacağını GÖSTER

---

## PROJE YAPISI (KISA)

```
Backend/
  SecretCustomer.API/      # Web API + MVC + Views
  SecretCustomer.Core/     # Entities, DTOs
  SecretCustomer.Data/     # EF Core, Migrations
  SecretCustomer.Services/ # Business Logic
```

- **Backend:** ASP.NET Core 9.0 + EF Core
- **Frontend:** KnockoutJS + Bootstrap 5
- **Database:** PostgreSQL
- **Port:** 5004
