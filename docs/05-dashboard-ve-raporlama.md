# Dashboard ve Raporlama Modülü

## Özet
Değerlendirmelerin (Evaluation) yönetimi, N/A puan hesaplama algoritması ve role-based dashboardlar.

## API Endpoints

### Evaluation
- `GET /api/evaluation/{id}` - Değerlendirme detayı
- `GET /api/evaluation/assignment/{assignmentId}` - Atamaya göre değerlendirme
- `GET /api/evaluation/evaluator/{evaluatorId}` - Değerlendirici'nin tüm değerlendirmeleri
- `POST /api/evaluation/start` - Değerlendirme başlat
- `POST /api/evaluation/submit` - Değerlendirme gönder (N/A hesaplaması ile)

### Dashboard
- `GET /api/dashboard/admin` - Yönetici dashboard (tüm veriler, trend, şube karşılaştırması)
- `GET /api/dashboard/teamleader/{branchId}` - Takım lideri dashboard (şube bazlı)
- `GET /api/dashboard/representative/{userId}` - Temsilci dashboard (kendi değerlendirmeleri)

## N/A Puan Hesaplama Algoritması

```csharp
// 1. Toplam maksimum puanı hesapla
decimal totalMaxPoints = allQuestions.Sum(q => q.Points);

// 2. N/A işaretli soruların puanlarını topla
var naPointsTotal = naQuestions.Sum(q => q.Points);

// 3. Adjusted maksimum puan (N/A çıkarıldıktan sonra)
decimal adjustedMaxPoints = totalMaxPoints - naPointsTotal;

// 4. Cevaplanan soruların kazanılan puanlarını topla
decimal totalEarned = answers.Where(a => !a.IsNA).Sum(a => a.EarnedPoints);

// 5. Yüzde hesapla
decimal percentage = (totalEarned / adjustedMaxPoints) * 100;
```

**Örnek:**
- Soru 1: 20 puan → Kazanıldı: 15 puan
- Soru 2: 30 puan → **N/A**
- Soru 3: 50 puan → Kazanıldı: 40 puan

**Hesaplama:**
- Orijinal Total: 100
- N/A Total: 30
- **Adjusted Max: 70**
- Earned: 55
- **Percentage: 78.57%**

## Dashboard İstatistikleri

### Admin Dashboard
```json
{
  "totalEvaluations": 150,
  "averageScore": 78.5,
  "percentageChange": 5.2,
  "topBranches": [...],
  "bottomBranches": [...],
  "monthlyTrends": [...],
  "branchComparisons": [...]
}
```

### Team Leader Dashboard
Sadece kendi şubesi için:
- Total evaluations
- Average score
- Percentage change
- Monthly trends

### Representative Dashboard
```json
[
  {
    "id": "guid",
    "projectName": "2026 Q1",
    "branchName": "Merkez Şube",
    "scorePercentage": 85.5,
    "completedAt": "2025-11-22"
  }
]
```

## Domain Modeli

### Evaluation
- **Status**: Pending, InProgress, Completed
- **TotalScore**: Kazanılan puan
- **MaxScore**: N/A çıkarıldıktan sonraki max puan
- **ScorePercentage**: Yüzde skoru

### Answer
- **AnswerText**: Metin cevabı
- **AnswerNumeric**: Likert/Star için sayısal cevap
- **IsNA**: N/A işareti
- **EarnedPoints**: Kazanılan puan

## Puan Hesaplama Tipleri

| Soru Tipi | Hesaplama |
|-----------|-----------|
| **MultipleChoice** | Tam puan (doğru cevap) |
| **Likert (1-5)** | `(seçilen / 5) * puan` |
| **Star (1-5)** | `(yıldız / 5) * puan` |
| **Text** | Puansız |

---
**Oluşturulma Tarihi**: 2025-11-22
**Versiyon**: 1.0
