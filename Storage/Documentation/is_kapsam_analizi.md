
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Where-Object {$_.Path -like "*dotnet.exe"} | Stop-Process -Force


# İş Kapsam Belgesi Analizi - Gizli Müşteri/Değerlendirme Platformu

## Genel Bakış

Bu belge, kurumların çağrı merkezi, şube ve saha operasyonlarında yapılan gizli müşteri ziyaretleri, denetim kontrolleri ve memnuniyet anketlerini tek bir dijital platformda yönetmek için tasarlanmış bir sistem için teknik gereksinim dökümanıdır.

---

## Mevcut Proje Yapısı

### Backend (C:\Users\ahmet\source\repos\ScretCustomer\Backend)
```
Backend/
├── SecretCustomer.API/          # Web API (ASP.NET Core)
├── SecretCustomer.Core/         # Domain entities, interfaces
├── SecretCustomer.Data/         # EF Core, Repositories
├── SecretCustomer.Services/     # Business logic
└── SecretCustomer.Services.Tests/  # Unit tests
```

### Frontend (C:\Users\ahmet\source\repos\ScretCustomer\Frontend)
```
Frontend/wwwroot/
├── index.html                   # SPA entry point
├── js/
│   ├── app.js                  # Sammy.js routing
│   ├── services/
│   │   ├── api.service.js      # API çağrıları
│   │   └── auth.service.js     # Authentication
│   └── viewmodels/
│       ├── login.viewmodel.js
│       ├── dashboard.viewmodel.js
│       ├── checklist.viewmodel.js
│       ├── assignments.viewmodel.js
│       ├── evaluations.viewmodel.js
│       └── projects.viewmodel.js
├── templates/
│   ├── login.html
│   ├── dashboard.html
│   ├── checklists.html
│   └── ...
└── css/
    └── app.css
```

### Mevcut Teknoloji Stack
**Backend:**
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server (varsayılan)

**Frontend:**
- KnockoutJS 3.5.1 (MVVM)
- Sammy.js (Routing)
- Bootstrap 5
- jQuery 3.6.0
- Fetch API (AJAX)

**Mimari:**
- Single Page Application (SPA)
- RESTful API
- ViewModel pattern
- Hash-based routing (#/)

---

## Platform Modülleri

### 1. Kontrol Listesi (Form Builder) Modülü

**Temel Özellikler:**
- Farklı yapıda formlar oluşturma imkanı
- Puanlı ve puansız soru tipleri
- Bölümlere ayrılabilen form yapısı

**Desteklenen Soru Tipleri:**
- Çoktan Seçmeli
- Likert (1-5, 1-7 vb.)
- Yıldızlı değerlendirme
- Metin alanı
- Evet/Hayır
- N/A (Gerekmedi) opsiyonu

**Kritik Özellik - N/A Mantığı:**
- N/A işaretlenen sorular toplam hesaplamadan tamamen çıkarılır
- Soru ağırlığı devre dışı kalır
- Kalan sorulara oransal olarak yeniden dağıtılır (normalize edilir)
- Bu özellik adil puanlama için hayati önem taşır

**Ek Yetenekler:**
- Soru ve bölüm kopyalama
- Form versiyonlama (opsiyonel)
- Zorunlu/isteğe bağlı soru desteği
- Soru açıklaması ve ipucu ekleme
- Ağırlıklı puan sistemi

---

### 2. Atama Modülü

#### 2.1. İç Değerlendiricilere Atama

**Yönetici Yetenekleri:**
- Kontrol listelerini belirli değerlendiricilere atama
- Proje bazlı organizasyon
- Şube/lokasyon belirleme
- Son tarih takibi
- Çoklu atama (Excel ile toplu atama)

**İşlevsel Özellikler:**
- Her değerlendirici kendi paneline sahip
- Sadece atanan formları görüntüleme
- Tekil form linkleri (proje-değerlendirici ilişkisine göre)
- Süre takibi

#### 2.2. Dış Müşterilere Gönderim (Memnuniyet Anketi)

**Mantık:**
- Bireysel proje ataması yok
- Tek açık form linki
- E-posta, SMS ve QR kod ile dağıtım

**Kısıtlar:**
- Müşteri aynı formu sadece 1 kez doldurabilir
- Admin kayıt silip tekrar doldurma hakkı verebilir

**Kayıt Yapısı:**
- Müşteri kimliği (ID, telefon, mail)
- Form tarihi
- Soru bazlı cevaplar

---

### 3. Raporlama Modülü

**Admin Yetkileri:**
- Tüm ham veriyi Excel olarak indirme
- Tekil değerlendirmeleri görüntüleme:
  - Cevaplar
  - Soru bazlı puanlar
  - Toplam puan
  - Açıklamalar
  - Değerlendirici/şube bilgisi

**Filtreleme Seçenekleri:**
- Proje
- Şube
- Değerlendirici
- Tarih aralığı
- Bölge
- Form tipi

**PowerPoint Rapor Çıktısı:**
- Otomatik grafik oluşturma
- Aylık ortalama puan grafikleri
- Bölge/şube karşılaştırmaları
- En yüksek/en düşük performans listeleri
- Trend çizgileri

---

### 4. Dashboard Modülü

#### 4.1. Yönetici Ekranı

**KPI Alanları:**
- Toplam ziyaret sayısı
- Genel ortalama puan
- Bir önceki aya göre artış/azalış yüzdesi
- En yüksek puanlı ilk 5 şube
- En düşük puanlı son 5 şube

**Grafikler:**

1. **Aylık Performans Trend Grafiği**
   - Çizgi grafik
   - Son 12 ay verisi
   - Tooltip ile detaylı bilgi

2. **Bölge/Şube Karşılaştırma Grafiği**
   - Çubuk grafik
   - Şube veya bölge ortalama puanları
   - Bölge, tarih aralığı, proje adı filtreleri

#### 4.2. Takım Lideri Görünümü
- Yönetici ekranındaki tüm metrikler
- Sadece bağlı olduğu şubeler için

#### 4.3. Müşteri Temsilcisi Görünümü
- Sadece kendi değerlendirmelerini görür
- Başkasının verisine erişemez

---

## Teknik Gereksinimler

### Kullanıcı Rolleri
1. Admin
2. Takım Lideri
3. Değerlendirici (İç)
4. Müşteri Temsilcisi
5. Dış Müşteri (Anonim)

### Yetkilendirme & Güvenlik
- JWT veya OAuth2 tabanlı kimlik doğrulama
- API tabanlı modüler mimari
- Form linkleri tekil token ile korunmalı

### Veri Yapısı (Özet)

**Form:**
- FormID
- Form Adı
- Bölümler[]
- Sorular[] (Tip, Ağırlık, Puan, Zorunlu, N/A desteği)

**Atama:**
- AssignmentID
- Proje
- FormID
- Şube
- Değerlendirici
- Deadline
- Status

**Cevap:**
- ResponseID
- AssignmentID veya Anonim Müşteri ID
- Soru Cevapları[]
- Toplam Puan (N/A normalize edilmiş)

---

## Ek Özellikler

1. **Mobil Uyumluluk:** Mobil uyumlu form doldurma
2. **Offline Sync:** Offline doldurup online olunca senkronlama (opsiyonel)
3. **API Entegrasyonu:** CRM entegrasyonuna açık yapı
4. **Loglama:** Inspect/History - Form kim tarafından ne zaman dolduruldu

---

## Analiz ve Değerlendirme

### Güçlü Yönler

1. **Kapsamlı Modüler Yapı:** Form builder, atama, raporlama ve dashboard modüllerinin net ayrımı
2. **Esnek Form Tasarımı:** Çoklu soru tipi ve ağırlıklı puanlama sistemi
3. **Gelişmiş N/A Mantığı:** Adil puanlama için kritik özellik, matematiksel olarak normalize edilmiş sistem
4. **Rol Bazlı Erişim:** Farklı kullanıcı tipleri için özelleştirilmiş görünümler
5. **Kapsamlı Raporlama:** Excel, PowerPoint ve dashboard raporlama seçenekleri
6. **Güvenlik Odaklı:** JWT/OAuth2, token bazlı form koruması

### Teknik Dikkat Noktaları

1. **N/A Normalize Algoritması:**
   - En kritik gereksinim
   - Dinamik ağırlık yeniden dağıtımı gerektirir
   - Test senaryoları detaylıca oluşturulmalı

2. **Performans Optimizasyonu:**
   - Büyük veri setleri için raporlama optimizasyonu
   - Dashboard'da gerçek zamanlı KPI hesaplamaları
   - Excel/PowerPoint export işlemleri için kuyruk sistemi

3. **Veri Bütünlüğü:**
   - Müşteri başına tek form doldurma kısıtı
   - Form versiyonlama için geçmiş verilerle uyumluluk
   - Atama ve cevap ilişkilerinin tutarlılığı

4. **Mobil ve Offline:**
   - Senkronizasyon çakışma çözümü
   - Offline veri depolama güvenliği

### Önerilen Teknoloji Stack

**Backend:**
- .NET Core / ASP.NET Core (Mevcut proje yapısıyla uyumlu)
- Entity Framework Core
- SQL Server / PostgreSQL

**Frontend (Mevcut Stack ile Uyumlu):**
- **KnockoutJS 3.5.1** - MVVM pattern (MEVCUT)
- **Sammy.js** - Client-side routing (MEVCUT)
- **Bootstrap 5** - UI framework (MEVCUT)
- **jQuery** - DOM manipulation (MEVCUT)
- **Chart.js** - Dashboard grafikleri için (eklenecek)
- **SortableJS** - Form builder'da sürükle-bırak için (eklenecek)
- **Select2** - Gelişmiş dropdown'lar için (eklenecek)
- **LocalForage** - Offline storage için (opsiyonel)

**Güvenlik:**
- JWT Authentication
- Role-based Authorization (Claims-based)
- HTTPS/TLS

**Entegrasyon:**
- RESTful API
- SignalR (Gerçek zamanlı bildirimler için)
- Background Jobs (Hangfire/Quartz.NET - Rapor oluşturma için)

### KnockoutJS ile Uygulama Stratejisi

**1. Form Builder - KnockoutJS Yaklaşımı:**
```javascript
function FormBuilderViewModel() {
    var self = this;

    // Observable arrays for dynamic form structure
    self.sections = ko.observableArray([]);
    self.questionTypes = ['multipleChoice', 'likert', 'star', 'text', 'yesNo'];

    // Computed observable for total weight
    self.totalWeight = ko.computed(function() {
        var total = 0;
        ko.utils.arrayForEach(self.sections(), function(section) {
            ko.utils.arrayForEach(section.questions(), function(q) {
                total += parseFloat(q.weight()) || 0;
            });
        });
        return total;
    });

    // N/A normalize calculation
    self.calculateNormalizedScore = function(responses) {
        // Exclude N/A questions and redistribute weights
        // Implementation needed
    };
}
```

**2. Dashboard Grafikleri - Chart.js Entegrasyonu:**
```javascript
function DashboardViewModel() {
    var self = this;

    self.monthlyData = ko.observableArray([]);

    // When data changes, update chart
    self.monthlyData.subscribe(function(newData) {
        updateTrendChart(newData);
    });

    function updateTrendChart(data) {
        // Chart.js implementation
        var ctx = document.getElementById('trendChart').getContext('2d');
        new Chart(ctx, { /* config */ });
    }
}
```

**3. Observable Pattern for Real-time Updates:**
- KnockoutJS'in iki yönlü data binding özelliği form doldurma için ideal
- `ko.observableArray` ile dinamik soru ekleme/çıkarma
- `ko.computed` ile otomatik puan hesaplama
- Custom bindings ile drag-drop entegrasyonu

**4. Mevcut Routing Yapısına Eklenecek Route'lar:**
```javascript
// app.js içine eklenecek
this.get('#/form-builder', function() {
    if (!requireAuth() || !authService.isAdmin()) return;
    applyViewModel(new FormBuilderViewModel(), '/templates/form-builder.html');
});

this.get('#/form-builder/:id', function() {
    var id = this.params.id;
    applyViewModel(new FormBuilderViewModel(id), '/templates/form-builder.html');
});

this.get('#/reports', function() {
    if (!requireAuth()) return;
    applyViewModel(new ReportViewModel(), '/templates/reports.html');
});
```

**5. Component Yapısı (KnockoutJS Components):**
```javascript
// Question component
ko.components.register('question-item', {
    viewModel: function(params) {
        this.question = params.question;
        this.onDelete = params.onDelete;
    },
    template: '<div class="question-card">...</div>'
});
```

### Geliştirme Aşamaları Önerisi (Mevcut Stack ile)

**Faz 1 - Core Backend & Form Builder (4-6 hafta):**

*Backend:*
- Form entity ve CRUD API'ları (FormController)
- Section ve Question modelleri
- Ağırlık ve puan hesaplama servisleri
- Atama (Assignment) entity ve API'ları

*Frontend (KnockoutJS):*
- `FormBuilderViewModel` oluşturma
- Dinamik soru ekleme/çıkarma (ko.observableArray)
- Sürükle-bırak için SortableJS entegrasyonu
- Bootstrap modal'ları ile soru tip seçimi
- `/templates/form-builder.html` tasarımı
- Ağırlık hesaplama (ko.computed)

**Faz 2 - Atama & Değerlendirme (4-6 hafta):**

*Backend:*
- Response entity ve scoring algoritması
- **N/A normalize algoritması (kritik)**
- Excel export servisi (EPPlus/ClosedXML)
- Toplu atama API'ları

*Frontend (KnockoutJS):*
- `EvaluationViewModel` - Form doldurma
- N/A checkbox logic ve puan gösterimi
- `AssignmentListViewModel` - Atama yönetimi
- Excel toplu yükleme UI
- Progress tracking (ko.observable)
- Validation ve error handling

**Faz 3 - Dashboard & Raporlama (3-4 hafta):**

*Backend:*
- Dashboard KPI API'ları
- Aggregation queries (EF Core)
- PowerPoint export servisi (OpenXML SDK)
- Filtreleme endpoint'leri

*Frontend (KnockoutJS + Chart.js):*
- `ReportViewModel` oluşturma
- Chart.js entegrasyonu
- Observable subscribe pattern ile grafik güncelleme
- Filter component'leri (tarih, şube, proje)
- Export butonları ve indirme mekanizması

**Faz 4 - Enhancement & Polish (2-3 hafta):**

*Frontend:*
- Mobil responsive iyileştirmeleri
- Loading states ve skeleton screens
- LocalForage ile offline form kaydetme
- Toast notifications (Bootstrap)
- Keyboard shortcuts

*Backend:*
- Performance tuning (caching, indexing)
- API rate limiting
- Logging ve monitoring
- Background job'lar (Hangfire)

### Risk ve Zorluklar

**Backend Zorluklar:**
1. **N/A Normalize Karmaşıklığı:** Matematiksel algoritma test edilmeli, edge case'ler önemli
2. **PowerPoint Auto-Generation:** OpenXML SDK ile template tasarımı ve data mapping zorlu
3. **Ölçeklenebilirlik:** Binlerce kullanıcı ve form için performans testi şart
4. **Excel Import Validation:** Toplu atama sırasında veri doğrulama kritik

**Frontend (KnockoutJS) Zorlukları:**
1. **Nested Observable Arrays:** Form → Section → Questions yapısında performans
   - Çözüm: `ko.mapping` plugin kullanımı veya manual observable oluşturma
2. **Memory Leaks:** Observable subscription'ların dispose edilmesi
   - Çözüm: `self.dispose()` function pattern kullanımı
3. **Complex Computed Observables:** N/A hesaplaması gibi dinamik puanlama
   - Dikkat: Pure/impure computed'lar arası fark
4. **Form Builder Drag-Drop:** SortableJS ile KnockoutJS entegrasyonu
   - Observable array güncelleme timing'i önemli
5. **Chart.js Integration:** Observable değişince chart destroy/recreate gerekli
   - Memory leak riski var
6. **Large Form Performance:** 100+ sorulu formlarda rendering yavaşlayabilir
   - Çözüm: Virtual scrolling veya pagination
7. **Mobile Offline Sync:** LocalForage ile KnockoutJS observable sync
   - Conflict resolution stratejisi gerekli

**KnockoutJS Özel Dikkat Noktaları:**
- `ko.cleanNode()` unutulmamalı (app.js:24'de mevcut)
- Subscription dispose pattern'i her ViewModel'de implement edilmeli
- `ko.toJS()` performans maliyeti - sadece gerektiğinde kullan
- Custom bindings için lifecycle management
- IE11 desteği gerekiyorsa polyfill'ler ekle

---

## Sonuç

Bu belge, enterprise-level bir gizli müşteri değerlendirme platformu için kapsamlı ve detaylı bir gereksinim seti sunmaktadır. **Mevcut KnockoutJS + Sammy.js + Bootstrap stack'iniz bu gereksinimleri karşılamaya yeterlidir.**

### Kritik Başarı Faktörleri

**Teknik:**
1. **N/A Normalize Algoritması:** En kritik gereksinim - backend'de matematiksel doğruluk şart
2. **KnockoutJS Observable Management:** Memory leak'lerden kaçınmak için disposal pattern'leri
3. **Form Builder UX:** SortableJS entegrasyonu ve kullanıcı deneyimi
4. **Performance:** Büyük formlar ve dashboard'larda observable array optimizasyonu

**Mimari:**
- Mevcut ViewModel pattern'i koruyun - tutarlılık önemli
- API-first yaklaşım sürdürülebilir
- Sammy.js routing yapısı şu an yeterli (SPA için)

**Önerilen Ek Kütüphaneler (KnockoutJS Ekosistemi):**
- `knockout-mapping.js` - Complex nested object'ler için
- `knockout-validation.js` - Form validation için
- `Chart.js 3.x` - Dashboard grafikleri
- `SortableJS` - Drag & drop
- `Select2` - Advanced dropdowns
- `LocalForage` - Offline storage
- `Moment.js` / `date-fns` - Tarih işlemleri

**Test Stratejisi:**
- Unit tests: Backend servisleri (N/A algoritması özellikle)
- Integration tests: API endpoint'leri
- Manual UI tests: KnockoutJS binding'leri için (otomatize zor)
- Performance tests: 1000+ form, 100+ soru senaryoları

**Geliştirme Tahmini:**
- **Toplam:** 13-19 hafta (3-5 ay)
- **Minimum MVP:** Faz 1+2 = 8-12 hafta
- **Tam Özellikli:** Tüm fazlar = 13-19 hafta

**Kaynaklar:**
- KnockoutJS Docs: https://knockoutjs.com/documentation/
- Chart.js: https://www.chartjs.org/
- SortableJS: https://sortablejs.github.io/Sortable/
- ASP.NET Core Best Practices: Microsoft Docs

### Sonraki Adımlar

1. **Database Schema Tasarımı:** Form, Section, Question, Response entity'lerini detaylandır
2. **N/A Algoritması Proof of Concept:** Matematiksel formül ve test case'leri
3. **Form Builder Prototype:** KnockoutJS ile basit drag-drop demo
4. **API Contract Tanımı:** Swagger/OpenAPI ile endpoint'leri dokümante et

Mevcut proje yapınız sağlam temellere sahip. KnockoutJS eskimiş olsa da, MVVM pattern ve observable'lar bu tür form-ağırlıklı uygulamalar için hala çok uygun.
