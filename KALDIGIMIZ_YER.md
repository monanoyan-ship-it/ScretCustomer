# Kaldığımız Yer - 26 Aralık 2025

---

## YAPILACAKLAR

### 1. SMTP E-posta Entegrasyonu (Öncelikli)
- [ ] IEmailService interface
- [ ] SmtpEmailService implementasyonu
- [ ] appsettings.json SMTP ayarları
- [ ] E-posta şablonları (HTML)

### 2. Müşteri Personeli Davet Sistemi
- [ ] Personel oluşturulunca otomatik davet maili
- [ ] Davet tokeni oluşturma
- [ ] "Şifre Belirle" sayfası
- [ ] Hoşgeldin e-posta şablonu

### 3. Bildirim E-postaları
- [ ] Yeni atama bildirimi
- [ ] Değerlendirme tamamlandı bildirimi
- [ ] Şifre sıfırlama (zaten kısmen var)
- [ ] Toplu mail gönderimi

---

## TAMAMLANDI - GUID → int Dönüşümü

### Yapılan Değişiklikler

**ID Tipi Değişikliği başarıyla tamamlandı:**

1. **BaseEntity** - `Guid Id = Guid.NewGuid()` → `int Id` (auto-increment)
2. **Tüm Entity'ler** - ~40 entity'deki Guid foreign key'ler int'e çevrildi
3. **DTO'lar** - Tüm DTO'lardaki Guid property'ler int'e çevrildi
4. **Repository'ler** - 14 repository dosyasındaki 61+ parametre güncellendi
5. **Interface'ler** - Tüm service interface'leri güncellendi
6. **Service'ler** - Tüm service implementasyonları güncellendi
7. **Controller'lar** - 21+ controller dosyası güncellendi
8. **Test'ler** - UserServiceTests.cs Guid→int düzeltildi

### Migration

- Eski migration'lar silindi
- Yeni `InitialCreate` migration oluşturuldu
- **ÖNEMLİ:** Server'a deploy edildiğinde yeni veritabanı oluşturulacak

---

## Deploy Talimatları

1. Publish klasörünü sunucuya kopyala
2. Uygulama ilk çalıştığında migration otomatik uygulanacak
3. Admin kullanıcı: `admin / Admin@123`

---

## Sistem Akışı (Müşteri Talebi)

```
1. Müşteri tanımlama (Customer)
2. Değerlendirme formu oluşturma (Checklist - Inbound/Outbound/Yazılı)
3. Form-Müşteri ilişkilendirme + Değerlendirme dönemi açma (Project)
4. Kalite uzmanı atama (User - Evaluator rolü)
5. Firma temsilcileri ekleme (CustomerPersonnel)
6. Değerlendirme yapma (Evaluation)
7. Müşteri portalı erişimi (CustomerPortal - ayrı login)
```

---

## Mevcut Müşteri Portalı Özellikleri

| Özellik | Durum |
|---------|-------|
| CustomerPersonnel entity | ✅ |
| Ayrı login sistemi | ✅ |
| 4 farklı rol | ✅ |
| Excel import | ✅ |
| Davet e-postası | ❌ Yapılacak |
| Şifre belirleme linki | ❌ Yapılacak |

---

## Önceki Yapılanlar

### Almanca Lokalizasyon
- `resources.de.xml` tamamen çevrildi

### Kullanıcı Dil Tercihi
- `User.PreferredLanguageId` eklendi

### Production Seed
- Temiz kurulum için seed data hazır

---

## Teknik Detaylar

### Build Durumu
- 0 Error, 0 Warning
- Release publish hazır
