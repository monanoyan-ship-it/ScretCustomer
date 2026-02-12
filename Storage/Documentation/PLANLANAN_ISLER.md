# Planlanan İşler (Ertelenen Özellikler)
**Oluşturulma:** 2026-01-29
**Son Güncelleme:** 2026-01-29

Bu dosya, müşteri tarafından onaylanan ancak daha sonraya ertelenen özellikleri içerir.

---

## 1. Toplu Karne İndirme
**Kaynak:** YAPILACAKLAR #24
**Sayfa:** CustomerPortal/PersonnelReportCard
**Açıklama:** Birden fazla personelin karnesini toplu olarak indirme özelliği
**Detay:** Nasıl çalışacağı netleştirilecek

---

## 2. Eğitim Videoları - Anket Özelliği
**Kaynak:** YAPILACAKLAR #19
**Açıklama:** Eğitim videolarında izleme hakkı bitince "anlaşıldı mı?" anketi yapılması
**Detay:** Yeni bir özellik olarak planlanacak. Video izleme tamamlandığında kullanıcıya anket soruları gösterilecek.

---

## 3. Excel Import Şablonları
**Kaynak:** YAPILACAKLAR #17
**Açıklama:** Müşteri kendi başına Excel import yapmayacak, biz yapacağız
**Yapılacak:** Şablonların düzeltilmesi ve açıklamaların genişletilmesi (müşterinin anlaması için)
**Not:** Bu tamamen iptal değil - şablonlar iyileştirilecek

---

## 4. Bildirim Lokalizasyonu
**Kaynak:** Claude Code analizi (2026-02-12)
**Açıklama:** Bildirim title/message'ları DB'ye hardcoded Türkçe string olarak yazılıyor (20+ yer). Kullanıcı dil değiştirince eski bildirimler Türkçe kalıyor.
**Etkilenen Servisler:**
- AssignmentService: "Yeni Atama", "Atama Tamamlandı", "Atama İptal Edildi"
- EvaluationService: "Yeni Değerlendirme Tamamlandı", "Değerlendirme Taslağa Alındı", "Değerlendirme İptal Edildi"
- ProjectService: "Proje Ekibine Eklendi", "Proje Tamamlandı", "Proje İptal Edildi"
- ApprovalsApiController: "Yeni Onay Talebi", "Onay Kabul Edildi", "Onay Reddedildi"
- FieldWorkerService: "Yeni Ziyaret Tamamlandı"
- SupportRequestService: "Yeni Destek Talebi", "Destek Talebi Cevaplandı"
- PersonnelRequestService: GetResourceAsync kullanıyor ama çözülmüş metni yazıyor (key değil)
**Çözüm:** Title'a resource key kaydet (ör: `Notification.Assignment.New`), API response'da localize et. Message pragmatik olarak olduğu gibi kalabilir (dinamik parametreler içeriyor).

---

## Notlar

- Bu özellikler öncelikli iş listesinden çıkarılmıştır
- Müşteri ihtiyaç duyduğunda tekrar değerlendirilecektir
- İptal edilen işler bu listede yer almaz (#14 Boş Alan, #23 Destek Talep Erişimi gibi)
