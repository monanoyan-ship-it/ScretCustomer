# Kaldığımız Yer - 29 Ocak 2026

---

## 🚧 DEVAM EDEN İŞ: Training Quiz (Anket) Sistemi

### Özet
Video tamamlandıktan sonra gösterilecek basit quiz/anket sistemi. Evaluation/Checklist sisteminden **AYRI**. Soru ağırlığı yok, seçenek ağırlığı var.

### Tamamlanan
- [x] Entity'ler (TrainingQuiz, TrainingQuizQuestion, TrainingQuizOption, TrainingQuizResponse, TrainingQuizAnswer)
- [x] DTO'lar
- [x] Service (TrainingQuizService) + SubmitQuizAsync (puan hesaplama)
- [x] API Controller'lar (participant + external endpoint'ler)
- [x] Admin UI (/TrainingQuiz) - Quiz yönetimi
- [x] Video'ya quiz atama
- [x] İç katılımcı quiz sayfası (/CustomerPortal/TrainingQuiz/{participantId})
- [x] Dış katılımcı quiz sayfası (/Training/Quiz/{token})
- [x] Quiz.js - Soru gösterme, cevaplama, submit
- [x] Quiz.cshtml - UI tamamlandı
- [x] Dış katılımcı video tamamlama modal'ı
- [x] Atama listesine İç/Dış tip ayrımı (IsExternal)
- [x] **Admin formda "Doğru Cevap" checkbox → radio button değiştirildi** (tek doğru cevap)

### Kalan İşler
- [ ] Quiz sistemini TEST ET
- [ ] Ayrı hesaplama endpoint'i yaz (kullanıcı istedi)

---

## Puan Hesaplama Mantığı (ONAYLANDI)

```
TotalScore = Seçilen seçeneklerin WeightPoints toplamı
MaxPossible = Tüm doğru seçeneklerin WeightPoints toplamı
Yüzde = (TotalScore / MaxPossible) * 100
```

- 0 puan da doğru cevap olabilir (diğerlerinin puanını düşürmek için)
- Her soru için TEK doğru cevap (radio button ile)

---

## Entity Yapısı

### TrainingQuiz
- Id, TrainingVideoId (FK)
- Title, Description
- PassingScore (geçme notu %, opsiyonel)
- IsRequired (zorunlu mu?)
- ShuffleQuestions, ShuffleOptions
- ShowResults, IsActive

### TrainingQuizQuestion
- Id, TrainingQuizId (FK)
- Text, HelpText, Order
- QuestionTypeId (1=SingleChoice, 2=MultipleChoice)

### TrainingQuizOption
- Id, TrainingQuizQuestionId (FK)
- Text, Order
- WeightPoints (seçenek ağırlığı)
- IsCorrect

### TrainingQuizResponse
- Id, TrainingQuizId (FK)
- TrainingVideoParticipantId / TrainingVideoExternalParticipantId
- TotalScore, MaxPossibleScore, ScorePercentage, IsPassed
- StartedAt, CompletedAt, StatusId

### TrainingQuizAnswer
- Id, TrainingQuizResponseId (FK), TrainingQuizQuestionId (FK)
- SelectedOptionId (FK)
- EarnedPoints, IsCorrect

---

## API Endpoint'leri

### Admin (Quiz Yönetimi)
```
GET/POST   /api/training-quiz                    - Liste / Oluştur
GET/PUT/DEL /api/training-quiz/{id}              - Detay / Güncelle / Sil
GET        /api/training-quiz/by-video/{videoId} - Video'ya bağlı quiz
```

### Katılımcı
```
GET   /api/training-videos/participant/{id}/quiz       - Quiz al
POST  /api/training-videos/participant/{id}/quiz/start - Başlat
POST  /api/training-videos/participant/{id}/quiz/submit - Gönder

GET   /api/training-videos/external/{token}/quiz       - Dış katılımcı quiz
POST  /api/training-videos/external/{token}/quiz/start
POST  /api/training-videos/external/{token}/quiz/submit
```

---

## Akış

```
Video izleniyor
    ↓
Video tamamlandı (IsCompleted=true)
    ↓
Quiz var mı? ──NO──→ Eğitim tamamlandı
    ↓ YES
Quiz.IsRequired? ──NO──→ Quiz göster (opsiyonel) → Eğitim tamamlandı
    ↓ YES
Quiz modal göster (zorunlu)
    ↓
Sorular cevaplanır
    ↓
Puan hesaplanır (seçenek ağırlıklarından)
    ↓
PassingScore geçildi mi?
    ↓ YES                    ↓ NO
Eğitim başarılı       Tekrar denenebilir
```

---

## Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `Views/TrainingQuiz/Index.cshtml` | Admin quiz yönetimi |
| `wwwroot/js/TrainingQuiz/Index.js` | Admin JS |
| `Views/Training/Quiz.cshtml` | Dış katılımcı quiz sayfası |
| `wwwroot/js/Training/Quiz.js` | Quiz JS (soru gösterme, submit) |
| `Services/TrainingQuizService.cs` | Service (CRUD + hesaplama) |
| `Controllers/Api/TrainingQuizApiController.cs` | API endpoint'ler |
