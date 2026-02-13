# Gölge Müşteri Modülü - Analiz

## Kaynak: `subat2026golgemusteri.xlsx`

---

## Excel'den Çıkan Veriler

### İstatistikler
- **542 arama** planlanmış (Şubat 2026, 16 iş günü)
- **13 firma**, her birinin sabit telefon numarası var
- **17 unique telefon numarası** (bazı firmalar aynı numarayı paylaşıyor olabilir)
- **10 personel** + 1 "Kuponlu" kategorisi (ne olduğu sorulacak)
- **43 unique soru** - firma bazlı soru havuzu
- **317/542 cevap dolu** (geri kalanı henüz yapılmamış)
- Kişi başı günde **2-3 arama** (adil dağıtılmış)

### Firma Listesi ve Arama Sayıları
| Firma | Telefon | Arama Sayısı | Cep Tel Notu |
|-------|---------|-------------|--------------|
| Bilyoner | 8502720272 | 113 | CEP TEL |
| altiliganyan.com | 8502724272 | 44 | CEP TEL |
| Nesine | 0850 558 0 558 | 29 | - |
| Birebin | 0216 630 63 83 | 29 | - |
| İddaa | 4448686 | 29 | - |
| Misli | 0850 220 33 66 | 29 | - |
| Renault | 444 66 22 | 25 | - |
| Dacia | 444 99 44 | 13 | - |
| tjk.org | 4445855 | 12 | - |
| bitalih.com | 8502414343 | 11 | - |
| sonduzluk.com | 4447128 | 11 | - |
| atyarışı.com | 8502020558 | 11 | - |
| hipodrom.com | 2126222000 | 11 | - |

### Soru Dağılımı
- Bazı sorular **firmaya özel** (Renault: 17, Dacia: 10, Bilyoner: 14 unique soru)
- Bazı sorular **ortak** - aynı soru 6 firmaya kadar sorulmuş (bahis soruları)
- Firma başı 6-17 arası unique soru

### Personel Dağılımı
- 10 kişi, her biri 13 firmaya arama yapıyor (bazıları 10-11)
- 16 iş günü boyunca çalışıyor (hafta içi)
- Günlük 1-4 arama arası (ortalama ~2.3)

---

## Modül Tasarımı

### Temel Prensipler
- **Tamamen bağımsız modül** - mevcut Evaluation/Assignment yapısından ayrı
- **Tek bağlantı noktası: Customer** - sadece müşteriye bağlı
- **Kendi entity'leri, kendi tabloları, kendi sayfaları**

### Veri Yapısı (Taslak)

#### 1. GolgeHedefFirma (Aranacak firmalar + numaralar)
- CustomerId (FK → Customer)
- FirmaAdi
- TelefonNo
- CepTelefonuGerekli (bool) → "cep telefondan aranacaklar" notu
- IsActive (bool) → ay başı planlama için aktif firmalar

#### 2. GolgeSoruHavuzu (Firma bazlı sorulacak sorular)
- CustomerId
- GolgeHedefFirmaId (FK) → nullable olabilir (ortak sorular için)
- SoruMetni
- BeklenenCevap (opsiyonel - referans için)
- IsActive

#### 3. GolgeAylikPlan (Her ay başı oluşturulan plan)
- CustomerId
- Ay / Yil (veya PlanBaslangic / PlanBitis)
- OlusturanKullaniciId
- Durum (Taslak / Aktif / Tamamlandi)
- CreatedAt

#### 4. GolgeAtama (Plan içindeki her bir arama görevi)
- GolgeAylikPlanId (FK)
- GolgeHedefFirmaId (FK) → hangi firma aranacak
- GolgeSoruHavuzuId (FK) → hangi soru sorulacak
- UserId (FK → User) → kim arayacak (bizim personelimiz, CustomerPersonnel DEĞİL)
- PlanTarihi (DateTime) → hangi gün
- GerceklesmeTarihi (DateTime?) → ne zaman yapıldı
- AramaSaati (string?) → saat bilgisi
- TemsilciIsmi (string?) → aranan taraftaki kişinin adı
- Cevap (text?) → alınan cevap
- Durum (Bekliyor / Tamamlandi / Cevaplanmadi / vb.)

### Sayfalar

#### Admin Tarafı
1. **Gölge Müşteri Firmaları** (`/GolgeMusteri/Firmalar`) - Firma + numara CRUD
2. **Soru Havuzu** (`/GolgeMusteri/Sorular`) - Firma bazlı soru yönetimi
3. **Aylık Plan Oluştur** (`/GolgeMusteri/Plan`) - Ay seç, personel seç, otomatik adil dağıtım yap
4. **Plan Takip** (`/GolgeMusteri/Takip`) - Tamamlanma durumu, istatistikler

#### User Tarafı (Admin panel - dinleme yapan personelimiz)
5. **Arama Listem** (`/GolgeMusteri/Aramalarim`) - Bugün/bu hafta/bu ay aramaları
6. **Arama Doldur** - Listeden tıklayınca: cevap yaz, temsilci adı yaz, durumu güncelle

### Aylık Plan Oluşturma Algoritması
1. Admin ay ve personelleri seçer
2. Aktif firmalar ve soruları çekilir
3. Firma-soru çiftleri hafta içi günlere dağıtılır
4. Personellere adil dağıtım yapılır (günlük eşit arama sayısı)
5. Admin planı inceleyip onaylar

---

## Sorulacak Sorular

### Müşteriye Sorulacak
1. **"Kuponlu" kategorisi ne?** - Excel'de 52 aramayla ayrı bir kategori. Otomatik sistem mi, farklı bir personel tipi mi?
2. **Soru havuzu yönetimi** - Sorular firmaya özel mi yoksa ortak havuzdan mı atanıyor? Admin mi belirliyor?
3. **Cep telefonu notu** - Bazı firmalar "cep telefonunuzdan arama yaparak PC'ye kaydetmenizi rica ediyorum" notu var. Bu bir firma özelliği mi?
4. **Temsilci ismi** - Aranan taraftaki kişinin adı kaydedilmeli mi?
5. **Hedef firma listesi** - Bu firmalar müşteriye mi bağlı yoksa dış bir talep edene mi bağlı?
6. **Raporlama** - Bu modülün kendi raporları olacak mı? (personel başarı oranı, firma bazlı istatistik vb.)

### Tasarım Kararları
- [ ] Firma listesi Customer'a bağlı (onaylandı)
- [ ] CallId / ses kaydı gerekmez (onaylandı)
- [ ] Tekrar arama mekanizması gerekmez (onaylandı)
- [ ] Aylık plan admin tarafından manuel oluşturulacak (onaylandı)
- [ ] "Kuponlu" ne olduğu öğrenilecek
- [ ] Soru havuzu yönetimi detayı öğrenilecek
