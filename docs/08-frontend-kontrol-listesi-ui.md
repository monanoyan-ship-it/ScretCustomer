# Frontend - Kontrol Listesi UI

## Özet
Kontrol listelerinin görüntülenmesi, oluşturulması, düzenlenmesi ve klonlanması için geliştirilmiş dinamik form arayüzü.

## Özellikler

### 1. Kontrol Listesi Yönetimi
- ✓ Liste görüntüleme
- ✓ Detay görüntüleme (read-only modal)
- ✓ Yeni kontrol listesi oluşturma
- ✓ Mevcut kontrol listesi düzenleme
- ✓ Kontrol listesi klonlama
- ✓ Dinamik bölüm ekleme/çıkarma
- ✓ Dinamik soru ekleme/çıkarma
- ✓ 4 farklı soru tipi desteği

### 2. Soru Tipleri
1. **YesNo**: Evet/Hayır soruları
2. **Rating**: 1-5 arası puanlama
3. **Text**: Metin cevapları
4. **MultipleChoice**: Çoktan seçmeli (seçeneklerle)

## Dosya Değişiklikleri

### index.html
İki yeni modal eklendi:

#### 1. Checklist Create/Edit Modal (`#checklistModal`)
```html
<div class="modal fade" id="checklistModal" tabindex="-1">
```

**Özellikler:**
- XL boyutlu modal (geniş form için)
- Dinamik başlık (Create/Edit moduna göre)
- Nested foreach loops (sections -> questions)
- Real-time validation
- Observable-based data binding

**Form Bölümleri:**
- **Temel Bilgiler**: Ad, Versiyon, Açıklama
- **Bölümler**: Dinamik section listesi
  - Her bölüm için: Ad, Sil butonu
  - Her bölümde: Soru listesi
- **Sorular**: Her soru için:
  - Soru metni
  - Soru tipi (dropdown)
  - Puan
  - N/A izin ver (checkbox)
  - Seçenekler (MultipleChoice için)
  - Sil butonu

#### 2. Checklist View Modal (`#checklistViewModal`)
```html
<div class="modal fade" id="checklistViewModal" tabindex="-1">
```

**Özellikler:**
- Read-only görüntüleme
- Bölümler ve sorular tablo formatında
- Badge'ler ile görsel bilgi
- Responsive tasarım

### checklists.js ViewModel

#### Observable Properties

```javascript
self.checklists = ko.observableArray([]);           // Tüm kontrol listeleri
self.isLoading = ko.observable(false);               // Liste yüklenme durumu
self.isSaving = ko.observable(false);                // Kaydetme durumu
self.isEditMode = ko.observable(false);              // Edit/Create mod

self.currentChecklist = ko.observable({              // Düzenlenen/oluşturulan
    id: null,
    name: ko.observable(''),
    description: ko.observable(''),
    version: ko.observable(1),
    sections: ko.observableArray([])
});

self.viewingChecklist = ko.observable(null);        // Görüntülenen
```

#### Fonksiyonlar

##### loadChecklists()
```javascript
await apiService.get('/checklist');
```
Tüm kontrol listelerini API'den yükler ve UI'ı günceller.

##### showCreateModal()
Yeni kontrol listesi oluşturmak için modal açar.
- `isEditMode = false`
- `currentChecklist` sıfırlanır
- Modal gösterilir

##### viewChecklist(checklist)
Kontrol listesi detaylarını read-only modal'da görüntüler.

##### editChecklist(checklist)
Kontrol listesini düzenlemek için modal açar.
- `isEditMode = true`
- `currentChecklist` doldurulur (deep copy)
- Tüm properties observable'a dönüştürülür
- Modal gösterilir

**Deep Copy İşlemi:**
```javascript
const sections = checklist.sections.map(section => ({
    id: section.id,
    name: ko.observable(section.name),
    order: section.order,
    questions: ko.observableArray(section.questions.map(question => ({
        id: question.id,
        text: ko.observable(question.text),
        questionType: ko.observable(question.questionType),
        points: ko.observable(question.points),
        allowNA: ko.observable(question.allowNA),
        options: ko.observable(question.options || ''),
        order: question.order
    })))
}));
```

##### cloneChecklist(checklist)
```javascript
await apiService.post(`/checklist/${checklist.id}/clone`);
```
Kontrol listesini klonlar, kullanıcıdan onay ister.

##### addSection()
Yeni bir boş section ekler:
```javascript
{
    id: null,
    name: ko.observable(''),
    order: sections.length + 1,
    questions: ko.observableArray([])
}
```

##### removeSection(section)
Onay isteyerek section'ı siler.

##### addQuestion(section)
Belirtilen section'a yeni soru ekler:
```javascript
{
    id: null,
    text: ko.observable(''),
    questionType: ko.observable('YesNo'),
    points: ko.observable(1),
    allowNA: ko.observable(false),
    options: ko.observable(''),
    order: questions.length + 1
}
```

##### removeQuestion(section, question)
Onay isteyerek soruyu siler.

##### saveChecklist()
Kontrol listesini kaydeder (create veya update).

**Validation:**
1. Name boş olmamalı
2. En az 1 section olmalı
3. Her section'da en az 1 soru olmalı

**DTO Hazırlama:**
```javascript
const dto = {
    name: current.name(),
    description: current.description(),
    version: current.version(),
    sections: current.sections().map((section, sIndex) => ({
        id: section.id,
        name: section.name(),
        order: sIndex + 1,
        questions: section.questions().map((question, qIndex) => ({
            id: question.id,
            text: question.text(),
            questionType: question.questionType(),
            points: parseFloat(question.points()),
            allowNA: question.allowNA(),
            options: question.options() || null,
            order: qIndex + 1
        }))
    }))
};
```

**API Calls:**
- Create: `POST /checklist`
- Update: `PUT /checklist/{id}`

**Success Flow:**
1. Modal kapatılır
2. Liste yeniden yüklenir
3. Success mesajı gösterilir

## KnockoutJS Data Binding Örnekleri

### Nested ForEach

```html
<!-- Sections loop -->
<div data-bind="foreach: currentChecklist().sections">

    <!-- Section input -->
    <input data-bind="value: name">

    <!-- Questions loop inside section -->
    <div data-bind="foreach: questions">
        <input data-bind="value: text">
    </div>
</div>
```

### Context Navigation ($parent, $parents)

```html
<!-- In questions loop, access parent (section) -->
<button data-bind="click: $parent.addQuestion">Soru Ekle</button>

<!-- In questions loop, access grandparent (viewmodel) -->
<button data-bind="click: function() { $parents[1].removeQuestion($parent, $data); }">
    Sil
</button>
```

### Conditional Visibility

```html
<!-- Show options field only for MultipleChoice -->
<div data-bind="visible: questionType() === 'MultipleChoice'">
    <input data-bind="value: options">
</div>

<!-- Show alert if no sections -->
<div data-bind="visible: currentChecklist().sections().length === 0">
    Bölüm eklenmemiş
</div>
```

### Dynamic Title

```html
<span data-bind="text: isEditMode() ? 'Kontrol Listesi Düzenle' : 'Yeni Kontrol Listesi'"></span>
```

## Bootstrap Modal Yönetimi

### Modal Initialization
```javascript
document.addEventListener('DOMContentLoaded', function() {
    const checklistModalEl = document.getElementById('checklistModal');
    checklistModal = new bootstrap.Modal(checklistModalEl);
});
```

### Modal Gösterme/Gizleme
```javascript
// Show
if (checklistModal) {
    checklistModal.show();
}

// Hide
if (checklistModal) {
    checklistModal.hide();
}
```

## Kullanıcı Akışları

### 1. Yeni Kontrol Listesi Oluşturma

```
1. "Yeni Kontrol Listesi" butonuna tıkla
2. Modal açılır (boş form)
3. Ad, versiyon, açıklama gir
4. "Bölüm Ekle" ile bölüm ekle
5. Bölüm adını gir
6. "Soru Ekle" ile soru ekle
7. Soru bilgilerini gir (metin, tip, puan, N/A)
8. MultipleChoice ise seçenekleri gir
9. İstediğin kadar bölüm/soru ekle
10. "Kaydet" butonuna tıkla
11. Validation geçerse API'ye gönder
12. Başarılıysa modal kapanır, liste güncellenir
```

### 2. Kontrol Listesi Düzenleme

```
1. Liste kartında "Düzenle" butonuna tıkla
2. Modal açılır (dolu form)
3. Değişiklikleri yap
   - Mevcut bölüm/soruları düzenle
   - Yeni bölüm/soru ekle
   - İstenmeyen bölüm/soruları sil
4. "Kaydet" butonuna tıkla
5. API'ye PUT request gönder
6. Başarılıysa modal kapanır, liste güncellenir
```

### 3. Kontrol Listesi Görüntüleme

```
1. "Görüntüle" butonuna tıkla
2. Read-only modal açılır
3. Tüm bölümler ve sorular tablo formatında görüntülenir
4. "Kapat" ile modal kapat
```

### 4. Kontrol Listesi Klonlama

```
1. "Klonla" butonuna tıkla
2. Confirmation dialog gösterilir
3. Onayla
4. API'ye POST request gönder
5. Başarılıysa liste yeniden yüklenir
6. Klonlanmış kontrol listesi listede görünür
```

## Validation Kuralları

### Frontend Validation:
- ✓ Kontrol listesi adı boş olamaz
- ✓ En az 1 bölüm olmalı
- ✓ Her bölümde en az 1 soru olmalı
- ✓ Soru metni boş olamaz
- ✓ Soru tipi seçilmeli
- ✓ Puan 0 veya pozitif olmalı
- ✓ HTML5 form validation (required attributes)

### Backend Validation (API tarafında):
- DTO validations
- Business rules
- Unique constraints

## UI/UX Özellikleri

### Responsive Design:
- Modal XL boyutunda
- Form grid system (col-md-*)
- Mobile-friendly inputs
- Scrollable modal body

### Visual Feedback:
- Loading spinners (kaydetme sırasında)
- Disabled buttons (loading state)
- Success/Error alerts
- Confirmation dialogs
- Badge indicators (version, section count)

### Color Coding:
- Primary: Görüntüle (mavi)
- Secondary: Düzenle (gri)
- Info: Klonla (açık mavi)
- Success: Bölüm Ekle (yeşil)
- Danger: Sil (kırmızı)

### Icons (Bootstrap Icons):
- `bi-plus-circle`: Yeni/Ekle
- `bi-eye`: Görüntüle
- `bi-pencil`: Düzenle
- `bi-files`: Klonla
- `bi-trash`: Sil
- `bi-save`: Kaydet
- `bi-check-circle-fill`: N/A izin var
- `bi-dash-circle`: N/A izin yok

## Performans Optimizasyonları

1. **Lazy ViewModel Creation**: ChecklistsViewModel sadece sayfa ilk ziyaret edildiğinde oluşturulur
2. **Deep Copy Only on Edit**: Sadece edit modunda deep copy yapılır
3. **Minimal Re-renders**: KnockoutJS observables sayesinde sadece değişen kısımlar güncellenir
4. **Modal Reuse**: Aynı modal hem create hem edit için kullanılır

## Örnek DTO (Save Request)

```json
{
  "name": "Restaurant Evaluation Checklist",
  "description": "Monthly restaurant evaluation form",
  "version": 1,
  "sections": [
    {
      "id": null,
      "name": "Temizlik",
      "order": 1,
      "questions": [
        {
          "id": null,
          "text": "Masalar temiz mi?",
          "questionType": "YesNo",
          "points": 5.0,
          "allowNA": false,
          "options": null,
          "order": 1
        },
        {
          "id": null,
          "text": "Genel temizlik puanı",
          "questionType": "Rating",
          "points": 10.0,
          "allowNA": true,
          "options": null,
          "order": 2
        }
      ]
    },
    {
      "id": null,
      "name": "Hizmet Kalitesi",
      "order": 2,
      "questions": [
        {
          "id": null,
          "text": "Karşılama nasıldı?",
          "questionType": "MultipleChoice",
          "points": 5.0,
          "allowNA": false,
          "options": "Mükemmel, İyi, Orta, Kötü",
          "order": 1
        },
        {
          "id": null,
          "text": "Ek yorumlar",
          "questionType": "Text",
          "points": 0.0,
          "allowNA": false,
          "options": null,
          "order": 2
        }
      ]
    }
  ]
}
```

## Testing Scenarios

### 1. Create Flow Test
```
- Boş form açılıyor mu?
- Bölüm eklenebiliyor mu?
- Soru eklenebiliyor mu?
- Validation çalışıyor mu?
- Kaydetme başarılı mı?
- Liste günceleniyor mu?
```

### 2. Edit Flow Test
```
- Form mevcut data ile dolduruluyor mu?
- Değişiklikler kaydediliyor mu?
- Bölüm/soru silinebiliyor mu?
- Update başarılı mı?
```

### 3. View Flow Test
```
- Tüm bölümler görüntüleniyor mu?
- Tüm sorular görüntüleniyor mu?
- Read-only mod çalışıyor mu?
```

### 4. Clone Flow Test
```
- Confirmation dialog gösteriliyor mu?
- Clone başarılı mı?
- Yeni item listede görünüyor mu?
```

### 5. MultipleChoice Test
```
- QuestionType değişince options field görünüyor mu?
- Options string doğru kaydediliyor mu?
```

### 6. N/A Feature Test
```
- Checkbox toggle çalışıyor mu?
- AllowNA değeri doğru kaydediliyor mu?
```

## Gelecek İyileştirmeler

- [ ] Drag & drop ile soru/bölüm sıralaması
- [ ] Bulk import (Excel'den kontrol listesi)
- [ ] Kontrol listesi şablonları (templates)
- [ ] Preview mode (değerlendirici görünümü)
- [ ] Version history ve rollback
- [ ] Soft delete (archive)
- [ ] Search ve filter functionality
- [ ] Pagination (çok sayıda kontrol listesi için)
- [ ] Toast notifications (alert yerine)
- [ ] Form validation with better UX (inline errors)
- [ ] Autosave (draft mode)

## Hatırlatmalar

1. **Modal cleanup**: Modal'lar her kapatılışta state'i temizlemeyi unutmayın
2. **Observable unwrapping**: API'ye göndermeden önce observable'ları unwrap edin
3. **Deep copy**: Edit modda deep copy yapmayı unutmayın (referans problemlerinden kaçının)
4. **Context**: Nested foreach'lerde doğru context'i kullanın ($parent, $parents)
5. **Validation**: Hem frontend hem backend validation yapın
6. **Error handling**: Try-catch ile hataları yakala ve kullanıcıya göster

---
**Frontend Kontrol Listesi UI Tamamlandı!**
- ✓ Liste görüntüleme
- ✓ Detay görüntüleme modal
- ✓ Create/Edit modal ile CRUD işlemleri
- ✓ Dinamik bölüm ve soru yönetimi
- ✓ 4 soru tipi desteği
- ✓ N/A özelliği
- ✓ Klonlama
- ✓ Validation
- ✓ Responsive tasarım

**Sonraki Adım**: Değerlendirme Formu UI geliştirme
