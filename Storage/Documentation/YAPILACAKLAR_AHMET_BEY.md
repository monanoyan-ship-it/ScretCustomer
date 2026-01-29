# Yapılacak İşler Listesi
**Kaynak:** Çalışma Ahmet Bey yeni.docx + Sonradan eklenen maddeler
**Tarih:** 2026-01-28
**Son Güncelleme:** Görsel analizi yapıldı

---

## Durum Açıklamaları
- ✅ Tamamlandı
- ⏸️ Beklemede (müşteriye sorulacak)

---

## İş Listesi

### 1. CustomerPortal/Penalties - Cezalı Değerlendirmeler
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Penalties
**Görsel:** image1
**Sorunlar:**
- [x] Göz açılmıyor - DTO'ya eksik alanlar eklendi
- [x] Excel'e dökümde alt kırılımlar eklendi
- [x] Periyot/ay eklendi - UI tabloya ve Excel "Cezalı Değerlendirmeler" tablosuna eklendi (Periyot varsa adı, yoksa YYYYMM formatı)

---

### 2. CustomerPortal/Reports - Puan Dağılımı
**Durum:** ✅ Tamamlandı (Müşteriye sorulacak)
**URL:** http://45.84.191.28/CustomerPortal/Reports
**Görsel:** image2
**Sorunlar:**
- [x] Tarih aralığı seçimi - MEVCUT
- [x] Üstüne tıklanınca detay görülecek - MEVCUT
- [ ] Ay geliyor ama çağrı adedi gelmiyor - **SORU: Hangi alanda? Ayrı tablo mu?** --YAPILDI
- [ ] Tüm projelerin aylık ortalaması - **SORU: Nasıl gösterilsin?** --YAPILDI

---

### 3. CustomerPortal/ExternalEvaluations - Dış Dinlemeler
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/ExternalEvaluations
**Görsel:** image3 (ID kopyala butonu kırmızı kutu ile işaretli)
**Sorunlar:**
- [x] Excel'e aktar hata veriyor - Düzeltildi
- [x] ID kopyala çalışmıyor - **CallId kısaltma ve kopyala butonu eklendi (commit 77e74bf)**

---

### 4. CustomerPortal/Suggestions - Öneriler (Kısım 1)
**Durum:** ✅ Kısmen Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Suggestions
**Görsel:** image4, image15
**Sorunlar:**
- [x] Değerlendirme sayısı ≠ öneri sayısı olmalı - **ZATEN MEVCUT: Backend'de SuggestionCount ve EvaluationCount ayrı hesaplanıyor, UI'da gösteriliyor**
- [x] En çok öneri yazılan sorular için kaç değerlendirmede bu değere ulaşıldı - **ZATEN MEVCUT: Aynı özellik**
- [x] Öneri listesinde not/öneri alanı özet gelsin - **YAPILDI: 100 karakter + detay butonu (popover) eklendi**
- [ ] Rapor bilgileri datası tabloya yansımamış - **SORU: Hangi data? Hangi tablo?** --TAMAMLANMIŞ

---

### 5. CustomerPortal/Suggestions - Alt Kırılımlar
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Suggestions
**Görsel:** image5 (Alt kriterler kırmızı kutu ile işaretli)
**Sorunlar:**
- [x] Öneriler alanında alt kırılımlar gelmeli - Alt kriterler eklendi --YAPILDI

---

### 6. CustomerPortal/ExternalEvaluations - Puan Hesaplama
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/ExternalEvaluations
**Sorunlar:**
- [ ] Puanlar yanlış hesaplanıyor - **SORU: Hangi değerlendirmede? Örnek lazım.** --YAPILDI

---

### 7. CustomerPortal/PersonnelReportCard - Temsilci Karnesi
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/PersonnelReportCard
**Sorunlar:**
- [x] Rapor getir hata veriyor - Düzeltildi
- [x] Global temsilcilerin güçlü/zayıf notları çıkmıyor - Düzeltildi
- [x] PerformanceSettings entegrasyonu - Proje tipine göre threshold değerleri alınıyor

---

### 8. CustomerPortal/Organizations - Aylık Gelişim
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Organizations
**Görsel:** image6 (Gelişim Trendi grafiği)
**Sorunlar:**
- [x] Aylık gelişim grafiğinde tarih düzeltildi - CallDate kullanılıyor 

---

### 9. Reports/SurveyResults - Anket Sonuçları
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/Reports/SurveyResults
**Görsel:** image7 (Excel tablosu - Yanıt, Ort.(%), Cevap, Seçim, Yüzde sütunları)
**Sorunlar:**
- [x] Cevap dağılımı tablosu eklendi - "Genel Soru Puan Dağılımı" altına yeni "Cevap Dağılımı" bölümü (Soru | Cevap | Seçim | Toplam | Oran)
- [x] Excel export'a "Cevap Dağılımı" sheet'i eklendi

---

### 10. CustomerPortal/Supervisors - Süpervizörler
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Supervisors
**Görsel:** image11
**Sorunlar:**
- [x] Organizasyon filtresi düzeltildi - Supervisor rol bazlı veri erişimi eklendi (commit 9249c24)

---

### 11. CustomerPortal/Dashboard - Puan Dağılımı Grafiği
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Dashboard
**Görsel:** image8 (Aylık Değerlendirme Trendi)
**Sorunlar:**
- [ ] Dönem seçilebilecek - **SORU: Hangi dönemler? Dropdown mı?** --yapıldı
- [ ] Renge tıklanınca dinlemeler listesi - **SORU: Modal mı? Liste mi?** --yapıldı
- [x] Filtrede projeler olmalı - Aylık Değerlendirme Trendi'ne proje filtresi eklendi (Soru Bazlı ile aynı liste)

---

### 12. Evaluations - Excel Export (Uzman)
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/Evaluations
**Görsel:** image9 (Personel sütunu boş - kırmızı kutu ile işaretli)
**Sorunlar:**
- [x] Excel'de personel ismi çıkmıyor - EvaluatedCustomerPersonnel eklendi
- [x] Filtreye proje eklenmeli - Proje filtresi eklendi
- [x] Excel şablonları çok uzun (CallId) - ExcelHelper.ApplyLongTextColumnStyles helper oluşturuldu. Tüm projede 40 Excel export'a uygulandı:
  - CallId sütunları: max 20 karakter genişlik (word wrap YOK)
  - Note/Öneri sütunları: max 20 karakter genişlik (word wrap YOK)
  - SubCriteria sütunları: max 20 karakter genişlik + word wrap

---

### 13. Evaluations - Filtreler (Kalite Uzmanı)
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/Evaluations
**Görsel:** image10 (Filtre dropdown - Proje yok)
**Sorunlar:**
- [x] Filtreye Proje eklenmeli - Proje filtresi eklendi
- [x] ID çok uzun çıkıyor - **CallId kısaltıldı (commit 77e74bf)**
- [x] Filtreye Periyot/Ay eklenmeli - YAPILDI
- [x] Atamalar alanına filtre eklenmeli - YAPILDI
- [x] Karnede indirme özelliği - Word export eklendi (NPOI). CustomerPortal/PersonnelReportCard'da "Word" butonu.

---

### 14. Değerlendirme Detayı - Boş Alan
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**Görsel:** image13 (Sağ tarafta kırmızı kutu ile işaretli BOŞ alan)
**Sorunlar:**
- [ ] "Şu alanda bir şey görmek istedim ama bulamadım" - **Görsel: Değerlendirme detayında sağ tarafta boş bir alan var. SORU: Bu alanda ne görünmeli?** --İptal edildi.

---

### 15. Öneriler Raporu - Alt Kriterler
**Durum:** ✅ Tamamlandı
**Görsel:** image16 (Seçilen alt kriterler kırmızı kutu ile işaretli)
**Sorunlar:**
- [x] "İşaretlediğim alanlar gelmeli" - Alt kriterler Suggestions raporuna eklendi

---

### 16. Personel Pasife Alma
**Durum:** ✅ Tamamlandı
**Sorunlar:**
- [ ] Pasif personel raporda görünmeli - **SORU: Hangi rapor?** --YAPILDI
- [ ] Değerlendirmede listede çıkmamalı - **SORU: Hangi sayfa?** --YAPILDI

---

### 17. Excel Import
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**Sorunlar:**
- [ ] Excel ile personel/kontrol listesi ekleme - **SORU: Mevcut özellik mi? Yeni mi?**--Bu yapılmayacak böyle bir şey olduğunda müşteri bizden isteyecek biz yapacağız. Yine de ne olur ne olmaz şablonları düzeltmemiz gerekiyor ve notlarını daha geniş yazıp müşterinin anlamasını sağlamamız gerekiyor.

---

### 18. Mail Sistemi - Karne Gönderimi
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**Sorunlar:**
- [ ] Değerlendirme kapatınca mail ile karneler gitmeli - **SORU: Detay lazım** -- Bununla ilgili çalışmayı tamamlayamadık. Bu sorun bizim sorunumuz sırası gelince bana sor.

---

### 19. Eğitim Videoları - Anket
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**Sorunlar:**
- [ ] Eğitim videolarına anket soru eklenebilir mi? - **SORU: Yeni özellik mi?** --Eğitim videolarında izleme hakkı bitince anlaşıldı mı diye anket yapmak istiyorlar. Bu yeni bir özellik ve daha sonraya planlanacak. Planlanan işler dosyasına eklenmeli.

---

### 20. Genel UI - Renk Değişikliği
**Durum:** ✅ Tamamlandı
**Sorunlar:**
- [x] Tüm sistemdeki renkler değişecek - Menüler/sidebar: #404040 (koyu gri), Panel başlıkları/tablo başlıkları/tablar: #fba92d (turuncu). app.css ve customer-layout.css'e global override'lar eklendi.

---

### 21. Grafik Export
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/dashboard
**Sorunlar:**
- [x] Grafikleri PDF olarak indirme - Dashboard'daki 3 grafiğe (Aylık Trend, Puan Dağılımı, Soru Trendi) PDF butonu eklendi. jsPDF + canvas.toDataURL() kullanılıyor. 

---

### 22. Excel Export - Sütun Düzeni
**Durum:** ✅ Tamamlandı
**Görsel:** image18 ("######" sorunu - sütunlar dar)
**Sorunlar:**
- [x] Excel sütun genişlikleri düzenlendi - Tarih sütunları için minimum genişlik eklendi

---

### 23. Destek Talep - Erişim Kısıtlama
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**Sorunlar:**
- [ ] Tüm kullanıcılara açılmasın - **SORU: Hangi kullanıcılar görebilmeli?** --Yapılmayacak

---

### 24. PersonnelReportCard - Toplu Karne
**Durum:** ⏸️ Beklemede (Müşteriye soruldu)
**URL:** http://45.84.191.28/CustomerPortal/PersonnelReportCard
**Sorunlar:**
- [ ] Toplu karne indirme - **SORU: Nasıl çalışmalı?** --Sonra Yapılacak planlanan işler dosyasına eklenmeli.

---

### 25. Eğitim Videoları - Sayfa Açılmıyor
**Durum:** ⏸️ Beklemede (Müşteriye sorulacak)
**Sorunlar:**
- [ ] Dış kullanıcı eğitim ataması - sayfa açılmadı - **SORU: Hangi sayfa? Hata mesajı?** --32 nci istekte detaylı anlatıldı.

---

### 26. Ford Concentrix - Video Ataması
**Durum:** ⏸️ Beklemede (Müşteriye sorulacak)
**Görsel:** image20 (Video atama modalı)
**Sorunlar:**
- [ ] Video ataması yapıldı ama personeller listele aktif değil - **SORU: Detay lazım** --Yapıldı

---

### 27. Customers/Organizations - Bağımsız Operatör Hatası
**Durum:** ✅ Tamamlandı
**URL:** https://localhost:5004/Customers/Organizations/{customerId}
**Sorunlar:**
- [x] "Yeni Oluştur" butonunda 400 hatası - role: int → string düzeltildi

---

### 28. CustomerPortal - İç/Dış Dinleme Ayrımı
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/InternalReports ve ExternalReports
**Sorunlar:**
- [x] API'lere isInternal parametresi eklendi
- [x] InternalReports ve ExternalReports sayfaları oluşturuldu
- [x] Menüye eklendi

---

### 29. CustomerPortal/Suggestions - Temsilci Adı
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/Suggestions
**Sorunlar:**
- [x] Cevap Bazlı Kayıtlarda temsilci adı gelmiyordu - EvaluatedCustomerPersonnel eklendi

---
### 29. İçeri aktarma şablonu
**Durum:** ⏸️ Beklemede
**URL:** http://45.84.191.28/Import
**Sorunlar:**
- Şablon yanlış geliyor. şablon açıklamaları da eksik.

---
### 30. Çağrı dinleme saatleri sorunu
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/Listenings
**Sorunlar:**
- [x] Çağrı denetleme raporunda çağrıyı dinleme tarih saatleri gelmiyor, gelmesi gerekiyor. -- Excel'e "Dinleme Tarihi" ve "Dinleme Saati" sütunları eklendi (CreatedAt kullanılıyor).

- 
---

### 31. Proje filtresi - Proje kodu eklenmesi
**Durum:** ✅ Tamamlandı
**URL:** http://45.84.191.28/CustomerPortal/ExternalEvaluations
**Sorunlar:**
- [x] hem dış hem iç dinlemelerde Proje filitresinde proje isimlerinin yanına "proje kodu" varsa eklenmeli. -- Dropdown ve aktif filtrelerde "Proje Adı (KOD)" formatında gösteriliyor.

---
### 32. Mail formatı sorunu
**Durum:** TEST
**URL:** http://45.84.191.28/TrainingVideos/Assignments
**Sorunlar:**
  - Bu sayfada atanan eğitim videoları mail formatı düzgün değil. mail formatını doldurma özelliği düzeltilmeli. ve link url ayrımı anket projelerindeki gibi yapılmalı.
  - TEST: `{TrainingVideoUrl}` placeholder'ı eklendi (sadece URL), `{TrainingVideoLink}` mevcut (tıklanabilir link). Hem iç hem dış katılımcı mailleri için placeholder değiştirme fonksiyonları güncellendi. Dış katılımcı video izleme sayfası mevcut: `/Training/External/{token}`

---

### 33. Dashboard geliştirmeleri
**Durum:** TEST
**URL:** http://45.84.191.28/dashboard
**Sorunlar:**
- Bu ay en çok dinleyenler panelindeki listeye detay butonu eklenmeli. detay butonuna tıklanınca o kişinin dinlediği çağrıların proje isimleri kodu ve sayısı gelmeli.
- Aylık değerlendirme trendindeki grafik firmalara göre de gösterilmeli. yani bizim toplamımız ve hangi firmaların kaç dinlemesi yapılmış.
- TEST: "Bu Ay En Çok Dinleyenler" tablosuna detay butonu eklendi (göz ikonu). Butona tıklanınca modal'da proje adı, kodu, firma ve adet gösteriliyor. "Bu Ay Firma Bazlı" paneli eklendi - firmalara göre değerlendirme sayıları ve ortalamalar listeleniyor.

---

## Özet

| Durum | Adet | Numaralar |
|-------|------|-----------|
| ✅ Tamamlandı | 15 | 1, 3, 5, 8, 10, 12, 13, 15, 22, 27, 28, 29 |
| ⏸️ Müşteriye sorulacak | 14 | 2, 4, 6, 7, 9, 11, 14, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26 |

---

## Görsel Referansları
Görseller: `C:\Users\Ahmet\Downloads\projeler\secretCustomer\AhmetBeyGorseller\`

| Görsel | Madde | Açıklama |
|--------|-------|----------|
| image1 | #1 | Penalties - Cezalı değerlendirmeler |
| image2 | #2 | Reports - Puan dağılımı |
| image3 | #3 | ExternalEvaluations - ID kopyala butonu |
| image4 | #4 | Suggestions - Not alanı |
| image5 | #5 | Alt Kriterler formu |
| image6 | #8 | Organizations - Gelişim Trendi |
| image7 | #9 | SurveyResults - Anket tablosu |
| image8 | #11 | Dashboard - Aylık trend |
| image9 | #12 | Excel - Personel sütunu boş |
| image10 | #13 | Evaluations - Filtre dropdown |
| image11 | #10 | Supervisors |
| image12 | - | Değerlendirme Detayı (kısa ID) |
| image13 | #14 | Değerlendirme Detayı - BOŞ alan |
| image14 | - | Personel seçimi |
| image15 | #4 | Suggestions sayfası |
| image16 | #15 | Alt kriterler (işaretli) |
| image17 | - | Sarı Kart - Alt kriter |
| image18 | #22 | Excel - Sütun genişliği |
| image19 | - | Excel açıklama formatı |
| image20 | #26 | Video atama modalı |

---

## ⚠️ CLAUDE İÇİN NOTLAR (SİLME!)

**Bu notları her session başında oku ve uygula:**

1. **ÖNCE ANLAT, ONAY AL, SONRA YAP** - Kod yazmadan önce ne anladığını açıkla, kullanıcı onay verince yap
2. **TEST işaretlemesi** - Checkbox'ı [x] yapma, sadece "TEST:" prefix ekle. Kullanıcı test edip onaylarsa [x] yap
3. **Notları SİLME** - Kullanıcının yazdığı notları değiştirme/silme, sadece başına TEST: ekle
4. **"Tüm projede" = TÜM PROJE** - Sadece bir klasöre bakma, grep ile tüm Backend/ klasörünü tara
5. **Analizi düzgün yap** - Önce tüm dosyaları bul, sonra sayıları ver, eksik bırakma
6. **Durumları güncelle** - İş bitince hem checkbox'ı hem "Durum:" satırını güncelle
7. **Hızlı değil DOĞRU** - Acele etme, düzgün analiz yap, onay al, sonra yap
