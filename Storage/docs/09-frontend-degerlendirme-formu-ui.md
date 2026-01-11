# Frontend - Değerlendirme Formu UI

## Özet
Değerlendiricilerin atanmış kontrol listelerini doldurarak değerlendirme yapabilecekleri dinamik form arayüzü. Real-time progress tracking, N/A desteği ve tüm soru tiplerini destekleyen kapsamlı bir modül.

## Özellikler

### 1. Değerlendirme Yönetimi
- ✓ Bekleyen atamaların listelenmesi
- ✓ Değerlendirme formunun dinamik yüklenmesi
- ✓ Real-time progress tracking (ilerleme çubuğu)
- ✓ 4 farklı soru tipi desteği
- ✓ N/A özelliği
- ✓ Genel notlar ekleme
- ✓ Form validation
- ✓ Değerlendirme gönderimi

### 2. Soru Tipleri UI
1. **YesNo**: Evet/Hayır buton grubu (yeşil/kırmızı)
2. **Rating**: 1-5 puan buton grubu (mavi)
3. **Text**: Çok satırlı textarea
4. **MultipleChoice**: Dropdown select (seçeneklerle)

## Dosya Değişiklikleri

### index.html

#### 1. Navigation Menu
Yeni menü öğesi eklendi:
```html
<li class="nav-item" data-bind="visible: isEvaluator() || isAdmin()">
    <a class="nav-link" href="#" data-bind="click: navigateTo.bind($data, 'evaluations')">
        <i class="bi bi-pencil-square"></i> Değerlendirmeler
    </a>
</li>
```
**Not**: Sadece Evaluator ve Admin rolleri için görünür.

#### 2. Evaluations Page
```html
<div id="evaluations-page" data-bind="visible: currentPage() === 'evaluations'">
```

**İki ana view:**

**A. Pending Assignments List View**
```html
<div data-bind="visible: !isEvaluating()">
```
- Bekleyen atamaların listesi
- Her atama için: Project adı, Şube, Kontrol Listesi, Deadline
- "Değerlendirmeye Başla" butonu
- Boş state: "Bekleyen değerlendirme bulunmamaktadır"

**B. Evaluation Form View**
```html
<div data-bind="visible: isEvaluating()">
```
- Card header: Proje adı, İptal butonu
- Progress bar: Cevaplanan/Toplam soru
- Sections foreach loop
  - Questions foreach loop
    - Soru metni, puan badge, N/A checkbox
    - Question type'a göre dinamik input
    - N/A mesajı
- Genel notlar textarea
- Gönder butonu

### evaluations.js ViewModel

#### Observable Properties

```javascript
self.pendingAssignments = ko.observableArray([]);    // Bekleyen atamalar
self.isLoading = ko.observable(false);                // Liste yükleme durumu
self.isEvaluating = ko.observable(false);             // Değerlendirme modunda mı?
self.isSubmitting = ko.observable(false);             // Gönderme durumu

self.currentAssignment = ko.observable(null);         // Aktif atama
self.evaluationSections = ko.observableArray([]);     // Kontrol listesi sections
self.notes = ko.observable('');                       // Genel notlar
```

#### Computed Properties

##### totalQuestions()
```javascript
self.totalQuestions = ko.computed(() => {
    let total = 0;
    self.evaluationSections().forEach(section => {
        total += section.questions.length;
    });
    return total;
});
```
Tüm bölümlerdeki toplam soru sayısını hesaplar.

##### answeredCount()
```javascript
self.answeredCount = ko.computed(() => {
    let answered = 0;
    self.evaluationSections().forEach(section => {
        section.questions.forEach(question => {
            if (question.answerIsNA()) {
                answered++;
            } else if (question.questionType === 'YesNo' || question.questionType === 'Rating') {
                if (question.answerNumeric() !== null) answered++;
            } else if (question.questionType === 'Text' || question.questionType === 'MultipleChoice') {
                if (question.answerText() && question.answerText().trim() !== '') answered++;
            }
        });
    });
    return answered;
});
```
Cevaplanan soru sayısını hesaplar (N/A dahil).

**Mantık:**
- N/A işaretlenmişse = cevaplandı
- YesNo/Rating: answerNumeric null değilse = cevaplandı
- Text/MultipleChoice: answerText boş değilse = cevaplandı

##### progressPercentage()
```javascript
self.progressPercentage = ko.computed(() => {
    const total = self.totalQuestions();
    if (total === 0) return 0;
    return Math.round((self.answeredCount() / total) * 100);
});
```
İlerleme yüzdesini hesaplar (0-100).

#### Fonksiyonlar

##### loadPendingAssignments()
```javascript
const data = await apiService.get(`/assignment/evaluator/${currentUser.userId}`);
const pending = data.filter(assignment =>
    assignment.status === 'Pending' || assignment.status === 'InProgress'
);
self.pendingAssignments(pending);
```

**Akış:**
1. Mevcut kullanıcının ID'sini al
2. Kullanıcının tüm atamalarını yükle
3. Sadece Pending ve InProgress olanları filtrele
4. Observable array'e kaydet

##### showNewEvaluationForm()
Yeni değerlendirme formu göstermek için pending assignments'ı yeniden yükler.
```javascript
self.showNewEvaluationForm = function() {
    self.loadPendingAssignments();
};
```

##### startEvaluation(assignment)
Bir atama için değerlendirme formunu yükler.

**Akış:**
1. Assignment'ın checklistId'sini kullanarak kontrol listesini yükle
2. Checklist sections ve questions'ı evaluation formatına dönüştür
3. Her soru için answer observable'ları ekle:
   - `answerText`: Text/MultipleChoice cevapları için
   - `answerNumeric`: YesNo/Rating cevapları için
   - `answerIsNA`: N/A flag

```javascript
const sections = checklist.sections.map(section => ({
    id: section.id,
    name: section.name,
    order: section.order,
    questions: section.questions.map(question => ({
        id: question.id,
        text: question.text,
        questionType: question.questionType,
        points: question.points,
        allowNA: question.allowNA,
        options: question.options,
        order: question.order,
        // Answer observables
        answerText: ko.observable(''),
        answerNumeric: ko.observable(null),
        answerIsNA: ko.observable(false)
    }))
}));
```

4. Current assignment ve sections'ı set et
5. isEvaluating = true

##### cancelEvaluation()
Değerlendirmeyi iptal eder.

**Akış:**
1. Confirmation dialog göster
2. Onayla
3. isEvaluating = false
4. State'i temizle (currentAssignment, evaluationSections, notes)

##### submitEvaluation()
Değerlendirmeyi API'ye gönderir.

**Validation:**
```javascript
let unansweredCount = 0;
self.evaluationSections().forEach(section => {
    section.questions.forEach(question => {
        const isNA = question.answerIsNA();
        const hasNumericAnswer = question.answerNumeric() !== null;
        const hasTextAnswer = question.answerText() && question.answerText().trim() !== '';

        if (!isNA) {
            if ((question.questionType === 'YesNo' || question.questionType === 'Rating') && !hasNumericAnswer) {
                unansweredCount++;
            } else if ((question.questionType === 'Text' || question.questionType === 'MultipleChoice') && !hasTextAnswer) {
                unansweredCount++;
            }
        }
    });
});

if (unansweredCount > 0) {
    alert(`Lütfen tüm soruları cevaplayın veya N/A işaretleyin. ${unansweredCount} soru cevaplanmamış.`);
    return;
}
```

**DTO Hazırlama:**
```javascript
const answers = [];
self.evaluationSections().forEach(section => {
    section.questions.forEach(question => {
        answers.push({
            questionId: question.id,
            answerText: question.answerText() || null,
            answerNumeric: question.answerNumeric(),
            isNA: question.answerIsNA()
        });
    });
});

const dto = {
    assignmentId: self.currentAssignment().id,
    evaluatorId: currentUser.userId,
    answers: answers,
    notes: self.notes() || null
};
```

**API Call:**
```javascript
await apiService.post('/evaluation/submit', dto);
```

**Success Flow:**
1. Success mesajı göster
2. State'i temizle
3. Pending assignments'ı yeniden yükle

### app.js Updates

#### New Computed Property
```javascript
self.isEvaluator = ko.computed(() =>
    self.currentUser() && self.currentUser().role === 'Evaluator'
);
```

#### ViewModel Management
```javascript
self.evaluationsViewModel = null;

// In navigateTo:
if (page === 'evaluations' && !self.evaluationsViewModel) {
    self.evaluationsViewModel = new EvaluationsViewModel();
}

if (page === 'evaluations' && self.evaluationsViewModel) {
    self.evaluationsViewModel.loadPendingAssignments();
}
```

## UI Components Detayları

### 1. Pending Assignments Card

```html
<div class="card mb-3 shadow-sm">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-start">
            <div>
                <h5 class="card-title" data-bind="text: projectName"></h5>
                <p class="card-text text-muted">
                    <strong>Şube:</strong> <span data-bind="text: branchName"></span><br>
                    <strong>Kontrol Listesi:</strong> <span data-bind="text: checklistName"></span><br>
                    <strong>Deadline:</strong> <span data-bind="text: new Date(deadline).toLocaleDateString('tr-TR')"></span>
                </p>
                <span class="badge bg-warning text-dark">Bekliyor</span>
            </div>
            <button class="btn btn-primary" data-bind="click: $parent.startEvaluation">
                <i class="bi bi-play-fill"></i> Değerlendirmeye Başla
            </button>
        </div>
    </div>
</div>
```

### 2. Progress Bar

```html
<div class="progress">
    <div class="progress-bar" role="progressbar"
         data-bind="style: { width: progressPercentage() + '%' }">
    </div>
</div>
```

**Özellikler:**
- Real-time güncelleme (computed property sayesinde)
- Yüzde hesabı otomatik
- Bootstrap progress bar styling

### 3. Question Types UI

#### A. YesNo Questions
```html
<div data-bind="visible: questionType === 'YesNo' && !answerIsNA()">
    <div class="btn-group w-100" role="group">
        <input type="radio" class="btn-check"
               data-bind="attr: { id: 'yes-' + id, name: 'q-yesno-' + id },
                          checked: answerNumeric,
                          checkedValue: 1">
        <label class="btn btn-outline-success"
               data-bind="attr: { for: 'yes-' + id }">
            <i class="bi bi-check-circle"></i> Evet
        </label>

        <input type="radio" class="btn-check"
               data-bind="attr: { id: 'no-' + id, name: 'q-yesno-' + id },
                          checked: answerNumeric,
                          checkedValue: 0">
        <label class="btn btn-outline-danger"
               data-bind="attr: { for: 'no-' + id }">
            <i class="bi bi-x-circle"></i> Hayır
        </label>
    </div>
</div>
```

**Özellikler:**
- Radio button group
- Evet = 1 (yeşil)
- Hayır = 0 (kırmızı)
- Icon'lar ile görsel feedback

#### B. Rating Questions
```html
<div data-bind="visible: questionType === 'Rating' && !answerIsNA()">
    <div class="btn-group w-100" role="group">
        <!-- ko foreach: [1, 2, 3, 4, 5] -->
        <input type="radio" class="btn-check"
               data-bind="attr: { id: 'rating-' + $parent.id + '-' + $data,
                                  name: 'q-rating-' + $parent.id },
                          checked: $parent.answerNumeric,
                          checkedValue: $data">
        <label class="btn btn-outline-primary"
               data-bind="attr: { for: 'rating-' + $parent.id + '-' + $data },
                          text: $data">
        </label>
        <!-- /ko -->
    </div>
    <small class="text-muted">1: Çok Kötü, 5: Mükemmel</small>
</div>
```

**Özellikler:**
- 1-5 arası radio buttons
- Konteksiz foreach (inline array)
- $parent ile parent question'a erişim
- Açıklayıcı small text

#### C. MultipleChoice Questions
```html
<div data-bind="visible: questionType === 'MultipleChoice' && !answerIsNA()">
    <select class="form-select" data-bind="value: answerText">
        <option value="">Seçiniz...</option>
        <!-- ko foreach: options ? options.split(',') : [] -->
        <option data-bind="text: $data.trim(), value: $data.trim()"></option>
        <!-- /ko -->
    </select>
</div>
```

**Özellikler:**
- Dropdown select
- Options string'i virgülle split edilir
- Trim ile boşluklar temizlenir
- Placeholder option

#### D. Text Questions
```html
<div data-bind="visible: questionType === 'Text' && !answerIsNA()">
    <textarea class="form-control" rows="3"
              data-bind="value: answerText"
              placeholder="Cevabınızı buraya yazın..."></textarea>
</div>
```

**Özellikler:**
- Çok satırlı textarea
- Placeholder text
- Form-control styling

### 4. N/A Feature

```html
<!-- N/A Checkbox -->
<div class="form-check" data-bind="visible: allowNA">
    <input type="checkbox" class="form-check-input"
           data-bind="checked: answerIsNA, attr: { id: 'na-' + id }">
    <label class="form-check-label" data-bind="attr: { for: 'na-' + id }">
        N/A
    </label>
</div>

<!-- N/A Message -->
<div data-bind="visible: answerIsNA()" class="alert alert-secondary">
    <i class="bi bi-info-circle"></i> Bu soru için N/A işaretlendi
</div>
```

**Mantık:**
- `allowNA` true ise checkbox görünür
- `answerIsNA()` true ise input alanları gizlenir
- N/A mesajı gösterilir
- Progress tracking'de N/A = cevaplandı olarak sayılır

## Kullanıcı Akışları

### 1. Değerlendirme Yapma Akışı

```
1. "Değerlendirmeler" sayfasına git
2. Bekleyen atamaların listesi görüntülenir
3. Bir atama için "Değerlendirmeye Başla"ya tıkla
4. Kontrol listesi yüklenir, form gösterilir
5. Her bölümü ve soruyu sırayla cevapla:
   - YesNo: Evet/Hayır seç
   - Rating: 1-5 arası puan ver
   - MultipleChoice: Dropdown'dan seç
   - Text: Textarea'ya yaz
   - N/A varsa ve uygunsa işaretle
6. Progress bar otomatik güncellenir
7. İsteğe bağlı genel notlar ekle
8. "Değerlendirmeyi Gönder"e tıkla
9. Tüm sorular cevaplandı mı kontrol edilir
10. Confirmation dialog gösterilir
11. API'ye gönderilir
12. Success mesajı gösterilir
13. Liste sayfasına dön
```

### 2. İptal Etme Akışı

```
1. Değerlendirme formu doldurulurken "İptal" butonuna tıkla
2. Confirmation dialog: "Girilen veriler kaydedilmeyecektir"
3. Onayla
4. Liste sayfasına dön
5. Tüm form verileri kaybolur
```

## Data Flow

### Assignment to Evaluation Form
```
Assignment Object:
{
    id: "guid",
    projectName: "Restaurant Evaluation",
    branchName: "İstanbul Kadıköy",
    checklistName: "Monthly Checklist",
    checklistId: "guid",
    deadline: "2025-12-31"
}

↓ Load Checklist

Checklist Object:
{
    id: "guid",
    name: "Monthly Checklist",
    sections: [
        {
            id: "guid",
            name: "Temizlik",
            questions: [
                {
                    id: "guid",
                    text: "Masalar temiz mi?",
                    questionType: "YesNo",
                    points: 5,
                    allowNA: false
                }
            ]
        }
    ]
}

↓ Transform to Evaluation Format

Evaluation Sections:
[
    {
        id: "guid",
        name: "Temizlik",
        questions: [
            {
                id: "guid",
                text: "Masalar temiz mi?",
                questionType: "YesNo",
                points: 5,
                allowNA: false,
                answerText: ko.observable(''),
                answerNumeric: ko.observable(null),
                answerIsNA: ko.observable(false)
            }
        ]
    }
]
```

### Evaluation Submission DTO
```json
{
  "assignmentId": "guid",
  "evaluatorId": "guid",
  "answers": [
    {
      "questionId": "guid",
      "answerText": null,
      "answerNumeric": 1,
      "isNA": false
    },
    {
      "questionId": "guid",
      "answerText": "Çok iyi hizmet",
      "answerNumeric": null,
      "isNA": false
    },
    {
      "questionId": "guid",
      "answerText": null,
      "answerNumeric": null,
      "isNA": true
    }
  ],
  "notes": "Genel olarak başarılı bir ziyaretti."
}
```

## Validation Rules

### Frontend Validation:
- ✓ Tüm sorular cevaplandı mı? (N/A dahil)
- ✓ YesNo/Rating: answerNumeric null olmamalı (eğer N/A değilse)
- ✓ Text/MultipleChoice: answerText boş olmamalı (eğer N/A değilse)
- ✓ Confirmation dialog (submission önce)

### Backend Validation (API):
- DTO validations ([Required] attributes)
- Business rules
- Assignment ownership check

## Performance Considerations

1. **Lazy ViewModel Creation**: EvaluationsViewModel sadece sayfa ilk ziyaret edildiğinde oluşturulur
2. **Computed Properties**: Progress tracking otomatik, manuel güncelleme gerektirmez
3. **Conditional Visibility**: Sadece aktif question type'ın UI'ı render edilir
4. **Minimal API Calls**: Checklist sadece bir kez yüklenir

## UI/UX Özellikleri

### Visual Feedback:
- Progress bar (real-time güncelleme)
- Loading spinners
- Disabled buttons (submission sırasında)
- Success/Error alerts
- Confirmation dialogs
- Badge indicators (puan, status)

### Color Coding:
- Success (Yeşil): Evet, Gönder butonu
- Danger (Kırmızı): Hayır
- Primary (Mavi): Rating, Başla butonu
- Warning (Sarı): Bekleyor badge
- Secondary (Gri): N/A mesajı

### Icons (Bootstrap Icons):
- `bi-play-fill`: Başla
- `bi-x-circle`: İptal, Hayır
- `bi-check-circle`: Evet
- `bi-send`: Gönder
- `bi-info-circle`: Bilgi
- `bi-pencil-square`: Değerlendirmeler menüsü

### Responsive Design:
- Progress bar full-width
- Button groups responsive
- Form controls responsive
- Cards stackable on mobile

## Testing Scenarios

### 1. Load Assignments Test
```
- API çağrısı yapılıyor mu?
- Pending assignments filtreleniyor mu?
- Boş liste durumu gösteriliyor mu?
- Card'lar doğru render ediliyor mu?
```

### 2. Start Evaluation Test
```
- Checklist yükleniyor mu?
- Sections ve questions doğru transform ediliyor mu?
- Answer observables oluşturuluyor mu?
- Form gösteriliyor mu?
```

### 3. Question Answering Test
```
- YesNo: Radio button binding çalışıyor mu?
- Rating: 1-5 seçimi kaydediliyor mu?
- MultipleChoice: Dropdown seçimi çalışıyor mu?
- Text: Textarea binding çalışıyor mu?
- N/A: Checkbox toggle çalışıyor mu?
```

### 4. Progress Tracking Test
```
- totalQuestions doğru hesaplanıyor mu?
- answeredCount güncelleniy or mu?
- progressPercentage 0-100 arasında mı?
- Progress bar görsel olarak güncelleniyor mu?
```

### 5. Validation Test
```
- Eksik cevap varsa uyarı gösteriliyor mu?
- N/A soruları cevaplandı olarak sayılıyor mu?
- Unanswered count doğru mu?
```

### 6. Submission Test
```
- DTO doğru oluşturuluyor mu?
- API çağrısı başarılı mı?
- Success mesajı gösteriliyor mu?
- State temizleniyor mu?
- Liste yeniden yükleniyor mu?
```

### 7. Cancel Test
```
- Confirmation dialog gösteriliyor mu?
- State temizleniyor mu?
- Liste sayfasına dönülüyor mu?
```

## Örnek Senaryo: Tamamlanmış Değerlendirme

**Assignment:**
- Project: "Restaurant Monthly Evaluation"
- Branch: "İstanbul Kadıköy"
- Checklist: "Standard Restaurant Checklist"
- Deadline: "2025-12-15"

**Checklist Sections:**

**1. Temizlik (3 soru)**
- Masalar temiz mi? (YesNo, 5 puan) → Evet
- Tuvalet temizliği (Rating, 10 puan) → 4
- Ek gözlemler (Text, 0 puan) → "Genel temizlik iyi"

**2. Hizmet Kalitesi (2 soru)**
- Karşılama nasıldı? (MultipleChoice, 5 puan) → "Mükemmel"
- Sipariş süresi (Rating, 10 puan) → N/A (işaretlendi)

**Genel Notlar:**
"Personel çok ilgiliydi. Temizlik standartları yüksek."

**Progress:**
- Total: 5 soru
- Answered: 5 (N/A dahil)
- Progress: 100%

**Submission DTO:**
```json
{
  "assignmentId": "assignment-guid",
  "evaluatorId": "evaluator-guid",
  "answers": [
    { "questionId": "q1-guid", "answerNumeric": 1, "answerText": null, "isNA": false },
    { "questionId": "q2-guid", "answerNumeric": 4, "answerText": null, "isNA": false },
    { "questionId": "q3-guid", "answerNumeric": null, "answerText": "Genel temizlik iyi", "isNA": false },
    { "questionId": "q4-guid", "answerNumeric": null, "answerText": "Mükemmel", "isNA": false },
    { "questionId": "q5-guid", "answerNumeric": null, "answerText": null, "isNA": true }
  ],
  "notes": "Personel çok ilgiliydi. Temizlik standartları yüksek."
}
```

## Gelecek İyileştirmeler

- [ ] Draft/Auto-save özelliği (evaluation yarıda kalırsa)
- [ ] Offline support (form doldurup sonra gönderme)
- [ ] Photo upload (soruya fotoğraf ekleme)
- [ ] Voice notes (sesli not kaydı)
- [ ] QR code scanning (şube check-in)
- [ ] GPS location tracking (ziyaret doğrulama)
- [ ] Time tracking (değerlendirme süresi)
- [ ] Previous evaluations görüntüleme
- [ ] Evaluation history/timeline
- [ ] Rich text editor (genel notlar için)
- [ ] Signature capture (dijital imza)
- [ ] PDF export (değerlendirme raporu)

## Hatırlatmalar

1. **N/A Logic**: N/A işaretlendiğinde input gizlenmeli ama cevaplandı sayılmalı
2. **Progress Tracking**: Computed properties otomatik güncellendiği için manuel trigger'a gerek yok
3. **Validation**: Hem frontend hem backend validation önemli
4. **Context**: Nested foreach'lerde $parent kullanımına dikkat
5. **State Management**: Submit sonrası state temizlemeyi unutma
6. **Error Handling**: API errors kullanıcıya anlaşılır şekilde gösterilmeli

---
**Frontend Değerlendirme Formu UI Tamamlandı!**
- ✓ Bekleyen atamaların listelenmesi
- ✓ Dinamik değerlendirme formu
- ✓ 4 soru tipi desteği (YesNo, Rating, Text, MultipleChoice)
- ✓ N/A özelliği
- ✓ Real-time progress tracking
- ✓ Form validation
- ✓ Değerlendirme gönderimi
- ✓ Responsive tasarım

**Sonraki Adım**: Atama Yönetimi UI geliştirme (Admin için)
