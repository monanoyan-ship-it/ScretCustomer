# Is Kurallari (Business Rules)

Bu dokuman, SecretCustomer sistemindeki is kurallarini, tanimlari ve sayfa bazinda islemleri detayli olarak aciklar.

---

## 1. Tanimlar (Glossary)

### 1.1 Temel Kavramlar

| Terim | Turkce | Aciklama |
|-------|--------|----------|
| **Customer** | Musteri | Hizmet verilen sirket/kurum |
| **CustomerPersonnel** | Musteri Personeli | Musterinin calisanlari (degerlendirilen kisiler) |
| **CustomerOrganization** | Organizasyon | Musterinin alt birimleri (sube, bolge, departman) |
| **Personnel** | Saha Calisani | Degerlendirme yapan kisiler (evaluator) |
| **User** | Sistem Kullanicisi | Admin panel kullanicilari |

### 1.2 Degerlendirme Kavramlari

| Terim | Turkce | Aciklama |
|-------|--------|----------|
| **Project** | Proje | Degerlendirme projesi (belirli musteri, checklist, tarih araligi) |
| **Checklist** | Kontrol Listesi | Degerlendirmede kullanilacak soru formu |
| **Question** | Soru | Checklist icindeki tek bir degerlendirme maddesi |
| **Group** | Grup | Sorularin kategorize edildigi baslik |
| **SubCriteria** | Alt Kriter | Bir sorunun alt maddeleri |
| **Assignment** | Atama | Bir degerlendirme gorevi (kim, kimi, ne zaman) |
| **Evaluation** | Degerlendirme | Tamamlanmis bir atama sonucu |
| **Answer** | Cevap | Bir soruya verilen yanit ve puan |

### 1.3 Atama ve Gorev Kavramlari

| Terim | Turkce | Aciklama |
|-------|--------|----------|
| **Internal Assignment** | Ic Atama | Sistem uzerinden yapilan degerlendirme |
| **External Assignment** | Dis Atama | QR kod/link ile yapilan degerlendirme |
| **AssignmentPeriod** | Atama Donemi | Degerlendirmelerin yapilacagi zaman araligi |
| **Listening** | Dinleme | Ses kaydi iceren degerlendirme |

### 1.4 Rol Kavramlari

| Terim | Turkce | Aciklama |
|-------|--------|----------|
| **Admin** | Yonetici | Tam yetkili sistem yoneticisi |
| **TeamLeader** | Takim Lideri | Saha calisanlari yoneticisi |
| **Evaluator** | Degerlendirici | Degerlendirme yapan saha calisani |
| **CustomerManager** | Musteri Yoneticisi | Musteri tarafinda en yetkili kisi |
| **CustomerSupervisor** | Supervisor | Musteri personeli yoneticisi |
| **CustomerOperator** | Operator | Standart musteri personeli |

### 1.5 Diger Kavramlar

| Terim | Turkce | Aciklama |
|-------|--------|----------|
| **N/A** | Uygulanamaz | Soru icin gecersiz/uygulanamaz durumu |
| **Weight** | Agirlik | Sorunun toplam puandaki etkisi |
| **Score** | Puan | Alinan deger |
| **Approval** | Onay | Degerlendirme onay sureci |
| **AuditLog** | Denetim Kaydi | Sistem islemlerinin kaydi |

---

## 2. Sayfa Bazinda Islemler

### 2.1 Dashboard (/Dashboard)

**Amac:** Sistem geneli ozet bilgiler

**Gosterilen Bilgiler:**
- Toplam degerlendirme sayisi
- Bu ayki degerlendirmeler
- Ortalama puan
- Aktif proje sayisi
- Aylik trend grafigi
- Puan dagilimi pasta grafigi
- Son degerlendirmeler listesi

**Rol Bazli Farklar:**
- Admin: Tum verileri gorur
- TeamLeader: Kendi takiminin verilerini gorur
- Evaluator: Kendi degerlendirmelerini gorur

---

### 2.2 Musteriler (/Customers)

**Amac:** Musteri yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Tum musterileri goruntule |
| Ekle | Yeni musteri olustur |
| Duzenle | Musteri bilgilerini guncelle |
| Sil | Musteriyi sil (soft delete) |

**Musteri Bilgileri:**
- Sirket Adi (CompanyName)
- Vergi No
- Adres
- Telefon
- Email
- Yetkili Kisi
- Aktif/Pasif durumu

---

### 2.3 Musteri Personeli (/CustomerPersonnel)

**Amac:** Musteri calisanlarinin yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Personelleri filtrele ve goruntule |
| Ekle | Yeni personel olustur |
| Duzenle | Personel bilgilerini guncelle |
| Sil | Personeli sil |
| Organizasyon Ata | Personeli organizasyona ekle |
| Supervisor Ata | Personele supervisor belirle |
| Sifre Sifirla | Personel sifresini sifirla |

**Personel Bilgileri:**
- Ad, Soyad
- Email, Telefon
- Musteri (hangi sirket)
- Rol (Manager/Supervisor/Operator/Viewer)
- Atandigi organizasyonlar
- Her organizasyon icin supervisor

**Ozel Kurallar:**
- Bir personel birden fazla organizasyona atanabilir
- Her organizasyon icin farkli supervisor olabilir
- Rol degisikligi veri erisimini etkiler

---

### 2.4 Musteri Organizasyonlari (/CustomerOrganizations)

**Amac:** Musteri alt birimlerinin yonetimi (sube, bolge, departman)

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Organizasyonlari goruntule |
| Ekle | Yeni organizasyon olustur |
| Duzenle | Organizasyon bilgilerini guncelle |
| Sil | Organizasyonu sil |
| Personel Ata | Organizasyona personel ekle |

**Organizasyon Bilgileri:**
- Organizasyon Adi
- Musteri
- Ust Organizasyon (hiyerarsi icin)
- Adres
- Aktif/Pasif

---

### 2.5 Projeler (/Projects)

**Amac:** Degerlendirme projelerinin yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Projeleri filtrele ve goruntule |
| Ekle | Yeni proje olustur |
| Duzenle | Proje bilgilerini guncelle |
| Sil | Projeyi sil |
| Dosya Yukle | Proje dosyasi ekle |
| Atama Yap | Projeye atama olustur |

**Proje Bilgileri:**
- Proje Adi
- Musteri
- Organizasyon (opsiyonel, bos ise tum org.)
- Checklist
- Baslangic/Bitis Tarihi
- Durum (Draft/Active/Completed/Archived)
- Aciklama

**Ozel Kurallar:**
- Bir projeye birden fazla personel (degerlendirici) atanabilir
- Proje olusturulurken organizasyon secilmisse, o projede yalnizca secilen organizasyondaki musteri personelleri degerlendirilir
- Organizasyon secilmemisse, musterinin tum organizasyonlarindaki personeller degerlendirilir

---

### 2.6 Kontrol Listeleri (/Checklists)

**Amac:** Degerlendirme formlarinin yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Checklistleri goruntule |
| Ekle | Yeni checklist olustur |
| Duzenle | Checklist ve sorulari duzenle |
| Sil | Checklisti sil |
| Klonla | Mevcut checklisti kopyala |
| Onizle | Formu onizle |

**Checklist Yapisi:**
```
Checklist
  └── Grup 1
        ├── Soru 1.1
        ├── Soru 1.2
        └── Soru 1.3
  └── Grup 2
        ├── Soru 2.1
        └── Soru 2.2
```

**Soru Tipleri:**
| Tip | Aciklama | Puanlama |
|-----|----------|----------|
| YesNo | Evet/Hayir | Evet=Max, Hayir=0 |
| Rating | 1-5 Puanlama | Secilen deger |
| Text | Acik metin | Puanlanmaz |
| MultipleChoice | Coktan secmeli | Secenek puani |
| SubCriteria | Alt kriterli | Alt kriter toplami |

**Ozel Kurallar:**
- Bir checklist birden fazla projede kullanilabilir
- Checklist bir projede kullanildiktan sonra degistirilirse, mevcut degerlendirmeler etkilenmez (cevaplar kayitli kalir)

---

### 2.7 Atamalar (/Assignments)

**Amac:** Degerlendirme gorevlerinin yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Atamalari filtrele ve goruntule |
| Tekil Atama | Tek bir atama olustur |
| Toplu Atama | Birden fazla atama olustur |
| Sil | Atamayi iptal et |
| Detay | Atama detaylarini gor |

**Atama Bilgileri:**
- Proje
- Degerlendirici (Personnel)
- Degerlendirilen (CustomerPersonnel)
- Organizasyon
- Planlanan Tarih
- Atama Tipi (Internal/External)
- Durum (Pending/InProgress/Completed/Cancelled)

**Filtreleme:**
- Tarihe gore
- Projeye gore
- Duruma gore
- Degerlendirici/Degerlendirilen'e gore

---

### 2.8 Degerlendirmeler (/Evaluations)

**Amac:** Tamamlanmis degerlendirmelerin goruntulenmesi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Degerlendirmeleri filtrele |
| Detay | Degerlendirme detayini gor |
| Export | Excel'e aktar |

**Detay Modali Icerigi:**
- Degerlendirme Bilgileri (tarih, puan, proje)
- Degerlendiren Bilgileri (personel adi)
- Degerlendirilen Bilgileri (firma, organizasyon, personel, supervisor)
- Cevap Tablosu:
  - Grup
  - Soru
  - Cevap
  - Agirlik
  - Kazanilan Puan
  - Notlar

---

### 2.9 Dinlemeler (/Listenings)

**Amac:** Ses kaydi iceren degerlendirmeler

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Dinlemeleri filtrele |
| Dinle | Ses kaydini oynat |
| Detay | Degerlendirme detayini gor |
| Export | Excel'e aktar |

**Ek Ozellikler:**
- Ses oynatici entegrasyonu
- Dinleme suresi gosterimi

---

### 2.10 Raporlar (/Reports)

**Amac:** Analiz ve raporlama

**Rapor Turleri:**
| Rapor | Icerik |
|-------|--------|
| Genel Rapor | Tum degerlendirmelerin ozeti |
| Performans | Personel bazli ortalamalar |
| Trend | Zaman bazli degisimler |
| Karsilastirma | Organizasyonlar arasi |

**Alt Sayfalar:**
- **/Reports/Suggestions** - Oneriler raporu
- **/Reports/Penalties** - Ceza/olumsuz maddeler
- **/Reports/PersonnelReportCard** - Personel karnesi

**Filtreleme:**
- Tarih araligi
- Musteri
- Organizasyon
- Proje
- Checklist
- Personel

---

### 2.11 Saha Calisanlari (/Personnel)

**Amac:** Degerlendirici personel yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Personelleri goruntule |
| Ekle | Yeni personel olustur |
| Duzenle | Bilgileri guncelle |
| Sil | Personeli sil |

**Personel Bilgileri:**
- Ad, Soyad
- Email, Telefon
- Adres
- Takim Lideri
- Aktif/Pasif

---

### 2.12 Kullanicilar (/Users)

**Amac:** Sistem kullanicisi yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Kullanicilari goruntule |
| Ekle | Yeni kullanici olustur |
| Duzenle | Bilgileri guncelle |
| Sil | Kullaniciyi sil |
| Sifre Sifirla | Sifre degistir |

**Kullanici Bilgileri:**
- Kullanici Adi
- Email
- Rol (Admin/TeamLeader/Evaluator/CustomerRepresentative)
- Aktif/Pasif

---

### 2.13 Yetkiler (/Permissions)

**Amac:** Rol ve kullanici bazli yetki yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Rol Yetkileri | Role izin ata/kaldir |
| Kullanici Yetkileri | Kullaniciya ozel izin |

**Yetki Yapisi:**
- Modul bazli (Customers.View, Customers.Edit, vb.)
- CRUD bazli (Create, Read, Update, Delete)

---

### 2.14 Egitimler (/Trainings)

**Amac:** Personel egitim takibi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Egitimleri goruntule |
| Ekle | Yeni egitim kaydi |
| Duzenle | Egitimi guncelle |
| Sil | Egitimi sil |

---

### 2.15 Toplantilar (/Meetings)

**Amac:** Toplanti yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Toplantilari goruntule |
| Ekle | Yeni toplanti olustur |
| Duzenle | Toplantiyi guncelle |
| Sil | Toplantiyi sil |

---

### 2.16 Onaylar (/Approvals)

**Amac:** Degerlendirme onay sureci

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Onay bekleyenleri gor |
| Onayla | Degerlendirmeyi onayla |
| Reddet | Degerlendirmeyi reddet |

---

### 2.17 Import (/Import)

**Amac:** Toplu veri yuklemesi

**Import Turleri:**
| Tip | Dosya | Aciklama |
|-----|-------|----------|
| Personel | Excel | Saha calisani toplu yukleme |
| Checklist | Excel | Kontrol listesi yukleme |
| Musteri Personeli | Excel | Musteri personeli yukleme |

---

### 2.18 Excel Sablonlari (/ExcelTemplates)

**Amac:** Excel export sablon yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Sablonlari goruntule |
| Ekle | Yeni sablon olustur |
| Duzenle | Kolon yapisini duzenle |
| Sil | Sablonu sil |

---

### 2.19 Dil Yonetimi (/Languages)

**Amac:** Coklu dil ve ceviri yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Dil Listesi | Aktif dilleri gor |
| Ceviri Ekle | Yeni ceviri kaydi |
| Ceviri Duzenle | Mevcut ceviriyi guncelle |
| Import | Excel'den ceviri yukle |
| Export | Cevirileri Excel'e aktar |

---

### 2.20 Ayarlar (/Settings)

**Amac:** Sistem ayarlari

**Ayar Kategorileri:**
- Genel ayarlar
- Email ayarlari
- Bildirim ayarlari
- Tema ayarlari

---

### 2.21 Denetim Kayitlari (/AuditLogs)

**Amac:** Sistem islemlerinin takibi

**Gosterilen Bilgiler:**
- Tarih/Saat
- Kullanici
- Islem tipi
- Etkilenen kayit
- Eski/Yeni degerler

---

### 2.22 Profil (/Profile)

**Amac:** Kullanici profil yonetimi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Bilgi Guncelle | Profil bilgilerini duzenle |
| Sifre Degistir | Sifre degistir |
| Bildirim Ayarlari | Bildirim tercihlerini ayarla |

---

## 3. Musteri Portali Sayfalari

### 3.1 Dashboard (/CustomerPortal/Dashboard)

**Amac:** Musteri personeli icin ozet ekran

**Gosterilen Bilgiler:**
- Toplam degerlendirme
- Ortalama puan
- Organizasyon sayisi
- Bu ayki degerlendirmeler
- Aylik trend grafigi
- Puan dagilimi
- Son degerlendirmeler

**Rol Bazli Veri Filtreleme:**
- CustomerManager: Tum veriler
- CustomerSupervisor: Takimi veya organizasyonu (ozel kural)
- CustomerOperator: Sadece kendisi

---

### 3.2 Degerlendirmeler (/CustomerPortal/Evaluations)

**Amac:** Degerlendirme gecmisi

**Yapilabilecek Islemler:**
| Islem | Aciklama |
|-------|----------|
| Listele | Degerlendirmeleri filtrele |
| Detay | Degerlendirme detayini gor |

---

### 3.3 Raporlar (/CustomerPortal/Reports)

**Amac:** Musteri icin raporlar

---

### 3.4 Subeler (/CustomerPortal/Branches)

**Amac:** Organizasyon bazli sonuclar

**Erisim:** Sadece CustomerManager

---

### 3.5 Profil (/CustomerPortal/Profile)

**Amac:** Musteri personeli profili

**Yapilabilecek Islemler:**
- Bilgileri goruntule
- Sifre degistir

---

## 4. Veri Erisim Kurallari

### 4.1 Sistem Kullanicilari

| Rol | Gorebilecegi Veriler |
|-----|---------------------|
| Admin | Tum sistem verileri |
| TeamLeader | Kendi takimindaki personeller ve onlarin degerlendirmeleri |
| Evaluator | Sadece kendi yaptigi degerlendirmeler |

### 4.2 Musteri Portali Kullanicilari

| Rol | Gorebilecegi Veriler |
|-----|---------------------|
| CustomerManager | Musteriye ait tum veriler |
| CustomerSupervisor | Ozel kural (asagida) |
| CustomerOperator | Sadece kendi degerlendirmeleri |
| CustomerViewer | Sadece kendi degerlendirmeleri (readonly) |

#### CustomerSupervisor Ozel Kurali

```
EGER altinda eleman VARSA:
  → Altindaki elemanlarin + kendi verilerini goster

EGER altinda eleman YOKSA ve organizasyona atanmis:
  → O organizasyondaki TUM personellerin verilerini goster
```

---

## 5. Puan Hesaplama Kurallari

### 5.1 Temel Formul

```
Her Soru Puani = (Alinan / Maximum) * Agirlik
Toplam Puan = Sum(Her Soru Puani)
Yuzde = (Toplam Puan / Toplam Agirlik) * 100
```

### 5.2 N/A Durumu

```
N/A isaretli sorular toplam agirliktan cikarilir
Yuzde = Alinan Puan / (Toplam Agirlik - N/A Agirliklari) * 100
```

### 5.3 Alt Kriterli Sorular

```
Soru Puani = Sum(Secilen Alt Kriterlerin Agirliklari)
Normalize = Soru Puani / Max Alt Kriter Toplami * Soru Agirligi
```

---

## 6. Durum Gecisleri

### 6.1 Atama Durumlari

```
        ┌─────────────────┐
        │     Pending     │
        └────────┬────────┘
                 │ Basla
        ┌────────▼────────┐
        │   InProgress    │
        └────────┬────────┘
                 │
        ┌────────┴────────┐
        │                 │
┌───────▼───────┐ ┌───────▼───────┐
│   Completed   │ │   Cancelled   │
└───────────────┘ └───────────────┘
```

### 6.2 Proje Durumlari

```
Draft → Active → Completed → Archived
```

---

## 7. Validasyon Kurallari

### 7.1 Zorunlu Alanlar

| Entity | Zorunlu Alanlar |
|--------|-----------------|
| Customer | CompanyName |
| CustomerPersonnel | FirstName, LastName, Email, CustomerId |
| Project | Name, CustomerId, ChecklistId |
| Assignment | ProjectId, EvaluatorId |

### 7.2 Format Kontrolleri

| Alan | Format |
|------|--------|
| Email | valid email format |
| Telefon | numerik, min 10 karakter |
| Tarih | YYYY-MM-DD |

### 7.3 Sifre Kurallari

- Minimum 6 karakter
- En az 1 buyuk harf
- En az 1 rakam
