# Günlük Dinleme Raporu - Analiz

## Konsept
Her Evaluation için, her Answer ayrı bir satır olacak. Bir değerlendirmede 20 soru varsa, 20 satır oluşur. Tüm bilgiler tek satırda "flatten" edilmiş halde.

## Filtre
- Sadece `Evaluation.CreatedAt` tarihine göre (verilen gün)
- Başka filtre yok

## Sütun Yapısı

| # | Sütun | Kaynak | Açıklama |
|---|-------|--------|----------|
| **DEĞERLENDİRME BİLGİLERİ** |
| 1 | DeğerlendirmeId | `Evaluation.Id` | |
| 2 | Durum | `Evaluation.StatusId` | Completed, Draft, vb. |
| 3 | Oluşturma Tarihi | `Evaluation.CreatedAt` | Filtre kriteri |
| 4 | Tamamlanma Tarihi | `Evaluation.CompletedAt` | |
| **PROJE / ATAMA** |
| 5 | Proje Adı | `Assignment.Project.Name` | |
| 6 | Proje Kodu | `Assignment.Project.Code` | |
| 7 | Proje Tipi | `Assignment.Project.ProjectTypeId` | CallAuditing, MysteryShopping, vb. |
| 8 | Müşteri | `Assignment.Project.Customer.CompanyName` | |
| **CHECKLIST** |
| 9 | Checklist Adı | `Assignment.Checklist.Name` | |
| 10 | Checklist Tipi | `Assignment.Checklist.ChecklistTypeId` | |
| 11 | Puanlama Yöntemi | `Assignment.Checklist.ScoringMethodId` | Maximum / CriteriaTotal |
| **DÖNEM** |
| 12 | Dönem Adı | `AssignmentPeriod.Name` | Null olabilir |
| **DEĞERLENDİRİLEN** |
| 13 | Personel Adı | `EvaluatedCustomerPersonnel` veya `EvaluatedUnknownPersonnel` | Fallback zinciri |
| 14 | Organizasyon | `EvaluatedOrganization.Name` | |
| 15 | Bayi | `CustomerDealer.Name` | Ziyaretler için |
| **DEĞERLENDİRİCİ** |
| 16 | Değerlendirici | `Evaluator.FirstName + LastName` | |
| **ÇAĞRI BİLGİLERİ** (CallAuditing için) |
| 17 | Çağrı ID | `Evaluation.CallId` | |
| 18 | Çağrı Tarihi | `Evaluation.CallDate` | |
| 19 | Çağrı Saati | `Evaluation.CallTime` | |
| 20 | Süre | `Evaluation.Duration` | |
| **ZİYARET BİLGİLERİ** (FieldWorker için) |
| 21 | Ziyaret ID | `Evaluation.VisitId` | |
| 22 | Kontrol Tarihi | `Evaluation.ControlDate` | |
| 23 | Kontrol Saati | `Evaluation.ControlTime` | |
| **PUANLAMA** |
| 24 | Toplam Puan | `Evaluation.TotalScore` | |
| 25 | Max Puan | `Evaluation.MaxScore` | |
| 26 | Yüzde | `Evaluation.ScorePercentage` | |
| 27 | Sarı Kart | `Evaluation.YellowCardCount` | |
| 28 | Kırmızı Kart | `Evaluation.RedCardCount` | |
| **SORU BİLGİLERİ** |
| 29 | Soru Grubu | `Question.GroupName` | |
| 30 | Soru Sırası | `Question.Order` | |
| 31 | Soru Metni | `Question.Text` | |
| 32 | Soru Tipi | `Question.ScoringTypeId` | Scored, Unscored, Penalty |
| 33 | Max Puan (Soru) | `Question.MaxPoints` | |
| 34 | Ağırlık | `Question.WeightPoints` | |
| **CEVAP BİLGİLERİ** |
| 35 | Verilen Puan | `Answer.GivenPoints` | |
| 36 | Kazanılan Puan | `Answer.EarnedPoints` | |
| 37 | Cevap (Metin) | `Answer.AnswerText` | |
| 38 | Cevap (Sayısal) | `Answer.AnswerNumeric` | |
| 39 | N/A | `Answer.IsNA` | |
| 40 | Ceza Uygulandı | `Answer.IsPenaltyApplied` | |
| 41 | Ceza Tipi | `Answer.AppliedPenaltyTypeId` | YellowCard / RedCard |
| 42 | Notlar | `Answer.Notes` | |
| 43 | Öneri | `Answer.RecommendationNotes` | |
| **GENEL** |
| 44 | Değerlendirme Notu | `Evaluation.EvaluationComment` | |
| 45 | Genel Notlar | `Evaluation.Notes` | |

## Satır Sayısı Tahmini
- Günde ~100 değerlendirme
- Ortalama ~20 soru/checklist
- **Günlük ~2000 satır**

## Teknik Notlar

1. **Sorgu**: `Answers` tablosundan başlayıp tüm ilişkileri Include etmek en verimli yol
2. **Tarih filtresi**: `Evaluation.CreatedAt >= startOfDay && Evaluation.CreatedAt < startOfNextDay`
3. **Export formatı**: Excel (ClosedXML)
4. **Endpoint**: `GET /api/reports/daily-evaluation-export?date=2025-02-05`

## Olası Ek Alanlar
- `Evaluation.DescriptionsJson` - JSON array, parse edilmeli mi?
- `Answer.AttachmentPath` / `AttachmentFileName` - Dosya bilgisi gerekli mi?
- `Customer.Code` - Müşteri kodu?
- `EvaluatedCustomerPersonnel.EmployeeCode` - Personel sicil no (varsa)?
