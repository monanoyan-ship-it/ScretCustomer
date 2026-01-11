# Frontend - Temel Yapı ve Login Sistemi

## Özet
KnockoutJS ve Bootstrap 5.3 kullanılarak geliştirilmiş MVVM (Model-View-ViewModel) mimarili frontend yapısı.

## Teknolojiler

- **KnockoutJS 3.5.1**: MVVM data binding framework
- **Bootstrap 5.3.0**: UI framework
- **Chart.js 4.4.0**: Grafik ve chart kütüphanesi
- **Bootstrap Icons**: İkon kütüphanesi

## Dosya Yapısı

```
Frontend/wwwroot/
├── index.html                    # Ana HTML dosyası
├── css/
│   └── app.css                   # Özel stil dosyası
└── js/
    ├── app.js                    # Ana uygulama ViewModel
    ├── services/
    │   ├── api.js                # HTTP request servisi
    │   └── auth.js               # Authentication servisi
    └── viewmodels/
        ├── dashboard.js          # Dashboard ViewModel
        └── checklists.js         # Kontrol Listeleri ViewModel
```

## API Service (api.js)

Base HTTP request handler. Tüm API isteklerini yönetir.

### Özellikler:
- JWT token yönetimi (LocalStorage'dan otomatik okuma)
- 401 Unauthorized durumunda otomatik logout
- Error handling
- Content-Type ve Authorization header yönetimi

### Kullanım:
```javascript
// GET request
const data = await apiService.get('/checklist');

// POST request
const response = await apiService.post('/auth/login', {
    username: 'admin',
    password: 'password123'
});

// PUT request
await apiService.put('/checklist/guid', updateData);

// DELETE request
await apiService.delete('/checklist/guid');
```

### Base URL Konfigürasyonu:
```javascript
const BASE_URL = 'https://localhost:7001/api';
```
**NOT**: Bu URL'yi API sunucunuzun adresine göre değiştirin.

## Authentication Service (auth.js)

Kimlik doğrulama işlemlerini yönetir.

### Fonksiyonlar:

#### login(username, password)
```javascript
const response = await authService.login('admin', 'password123');
// Returns: { token, userId, username, fullName, role, branchId }
```

#### register(userData)
```javascript
const response = await authService.register({
    username: 'newuser',
    email: 'user@example.com',
    password: 'password123',
    firstName: 'John',
    lastName: 'Doe',
    role: 'Evaluator',
    branchId: 'guid'
});
```

#### logout()
LocalStorage'dan token ve user bilgilerini temizler.

#### getCurrentUser()
Mevcut kullanıcı bilgilerini döndürür.

#### isAuthenticated()
Kullanıcının giriş yapıp yapmadığını kontrol eder.

#### isAdmin(), isTeamLeader(), isEvaluator()
Kullanıcının rolünü kontrol eder.

### LocalStorage Kullanımı:
```javascript
// Stored data
localStorage.setItem('token', 'jwt-token-here');
localStorage.setItem('currentUser', JSON.stringify({
    userId: 'guid',
    username: 'admin',
    fullName: 'Admin User',
    role: 'Admin',
    branchId: null
}));
```

## Dashboard ViewModel (dashboard.js)

Dashboard sayfasının verilerini yönetir.

### Observable Properties:
- `stats`: Dashboard istatistikleri
  - totalEvaluations: Toplam değerlendirme sayısı
  - averageScore: Ortalama puan
  - percentageChange: Yüzdelik değişim
  - topBranches: En iyi 5 şube
  - bottomBranches: Geliştirilmesi gereken 5 şube
- `isLoading`: Yükleme durumu

### Fonksiyonlar:

#### loadDashboardData()
Kullanıcının rolüne göre uygun dashboard endpoint'ini çağırır:
- Admin: `/dashboard/admin`
- TeamLeader: `/dashboard/teamleader/{userId}`
- CustomerRepresentative: `/dashboard/representative/{branchId}`

## Checklists ViewModel (checklists.js)

Kontrol listeleri sayfasını yönetir.

### Observable Properties:
- `checklists`: Kontrol listesi array'i
- `isLoading`: Yükleme durumu

### Fonksiyonlar:

#### loadChecklists()
Tüm kontrol listelerini yükler.

#### viewChecklist(checklist)
Kontrol listesi detaylarını görüntüler (TODO).

#### editChecklist(checklist)
Kontrol listesini düzenler (TODO).

#### cloneChecklist(checklist)
Kontrol listesini klonlar.
```javascript
await apiService.post(`/checklist/${checklist.id}/clone`);
```

#### showCreateModal()
Yeni kontrol listesi oluşturma modalını gösterir (TODO).

## Main App ViewModel (app.js)

Ana uygulama mantığını ve routing'i yönetir.

### Observable Properties:

#### Authentication State:
- `isAuthenticated`: Kullanıcı giriş yapmış mı?
- `currentUser`: Mevcut kullanıcı bilgileri
- `isAdmin`: Admin yetkisi var mı?
- `isTeamLeader`: Team Leader yetkisi var mı?

#### Navigation:
- `currentPage`: Aktif sayfa ('login', 'dashboard', 'checklists', 'projects', 'assignments')

#### Login Form:
- `loginUsername`: Kullanıcı adı
- `loginPassword`: Şifre
- `loginError`: Hata mesajı

#### ViewModels:
- `dashboardViewModel`: Dashboard ViewModel instance
- `checklistsViewModel`: Checklists ViewModel instance

### Fonksiyonlar:

#### login()
```javascript
await authService.login(username, password);
// Update state and navigate to dashboard
```

#### logout()
```javascript
authService.logout();
// Clear state and navigate to login
```

#### navigateTo(page)
Sayfalar arası geçiş yapar.
- Authentication kontrolü yapar
- Admin yetkisi gerekli sayfalarda yetki kontrolü yapar
- ViewModel'leri lazy-load eder
- Sayfa değiştiğinde verileri yeniden yükler

#### init()
Uygulama başlatılır:
- LocalStorage'da token varsa dashboard'a yönlendirir
- Yoksa login sayfasını gösterir

## HTML Yapısı (index.html)

### Navigation Bar
```html
<nav class="navbar navbar-expand-lg navbar-dark bg-primary"
     data-bind="visible: isAuthenticated">
```
- Giriş yapılınca görünür
- Rol bazlı menü öğeleri (`data-bind="visible: isAdmin"`)
- Kullanıcı dropdown menüsü

### Login Page
```html
<div id="login-page"
     data-bind="visible: currentPage() === 'login'">
```
- Username/Password form
- Error message display
- Loading spinner

### Dashboard Page
```html
<div id="dashboard-page"
     data-bind="visible: currentPage() === 'dashboard'">
```
- 4 istatistik kartı (Toplam Değerlendirme, Ortalama Puan, Değişim)
- Top 5 şube listesi
- Bottom 5 şube listesi

### Checklists Page
```html
<div id="checklists-page"
     data-bind="visible: currentPage() === 'checklists'">
```
- Kontrol listesi kartları
- Her kart için: Görüntüle, Düzenle, Klonla butonları
- "Yeni Kontrol Listesi" butonu (Admin için)

### Assignments Page
```html
<div id="assignments-page"
     data-bind="visible: currentPage() === 'assignments'">
```
- Placeholder (geliştirilecek)

## KnockoutJS Data Binding Örnekleri

### Observable Binding:
```html
<span data-bind="text: currentUser().fullName"></span>
```

### Visible Binding:
```html
<div data-bind="visible: isAuthenticated">...</div>
```

### Click Binding:
```html
<button data-bind="click: logout">Çıkış</button>
<a data-bind="click: navigateTo.bind($data, 'dashboard')">Dashboard</a>
```

### ForEach Binding:
```html
<ul data-bind="foreach: checklists">
    <li data-bind="text: name"></li>
</ul>
```

### Value Binding:
```html
<input type="text" data-bind="value: loginUsername">
```

### Submit Binding:
```html
<form data-bind="submit: login">...</form>
```

### Enable Binding:
```html
<button data-bind="enable: !isLoading()">Giriş Yap</button>
```

## CSS Customizations (app.css)

### Özellikler:
- Card hover effects (transform + box-shadow)
- Gradient backgrounds for dashboard cards
- Button hover animations
- Form styling
- Responsive design (mobile breakpoints)
- Fade-in animations
- Custom shadow utilities

### Önemli Class'lar:
```css
.card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.btn:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}
```

## Uygulama Akışı

1. **Sayfa Yükleme**:
   - DOM ready event
   - AppViewModel oluşturulur
   - KnockoutJS bindings uygulanır
   - `init()` fonksiyonu çalışır

2. **Giriş Kontrolü**:
   - LocalStorage'da token var mı?
   - Varsa: Dashboard'a yönlendir
   - Yoksa: Login sayfası göster

3. **Login**:
   - Kullanıcı credentials girer
   - `authService.login()` çağrılır
   - Token ve user info LocalStorage'a kaydedilir
   - Dashboard'a yönlendirme

4. **Navigation**:
   - Navbar veya programatik `navigateTo()` çağrısı
   - Authentication ve authorization kontrolleri
   - Gerekirse ViewModel'ler oluşturulur
   - Veriler API'den yüklenir

5. **Logout**:
   - LocalStorage temizlenir
   - State sıfırlanır
   - Login sayfasına yönlendirme

## Security Considerations

- JWT token LocalStorage'da saklanır
- Her API request'te Authorization header eklenir
- 401 durumunda otomatik logout
- Role-based UI elements (admin-only pages)
- HTTPS kullanımı (production için)

## Gelecek Geliştirmeler

- [ ] Kontrol listesi detay görüntüleme
- [ ] Kontrol listesi oluşturma/düzenleme formları
- [ ] Değerlendirme formu UI
- [ ] Atama yönetimi UI
- [ ] Dashboard grafikleri (Chart.js entegrasyonu)
- [ ] Profil sayfası
- [ ] Token refresh mekanizması
- [ ] Error boundary/global error handling
- [ ] Toast notifications
- [ ] Loading states/skeletons

## Test Etme

### Manuel Test Adımları:

1. **Login Testi**:
   - Hatalı credentials ile deneyin
   - Doğru credentials ile giriş yapın
   - LocalStorage'ı kontrol edin

2. **Navigation Testi**:
   - Her menü öğesine tıklayın
   - Sayfa geçişlerini kontrol edin
   - Geri buton davranışını test edin

3. **Authorization Testi**:
   - Admin olmayan kullanıcı ile "Kontrol Listeleri"ne erişmeyi deneyin
   - Hata mesajı görünmeli

4. **Logout Testi**:
   - Çıkış yapın
   - LocalStorage temizlenmiş mi?
   - Login sayfasına yönlendirildiniz mi?

### Browser Console Komutları:
```javascript
// LocalStorage içeriğini görüntüle
localStorage.getItem('token');
localStorage.getItem('currentUser');

// Manual navigation
window.location.reload();
```

---
**Frontend Temel Yapı Tamamlandı!**
- ✓ API Service (HTTP requests)
- ✓ Authentication Service (Login/Logout)
- ✓ Dashboard ViewModel
- ✓ Checklists ViewModel
- ✓ Main App ViewModel (Routing)
- ✓ Responsive HTML Layout
- ✓ Custom CSS Styling

**Sonraki Adım**: Kontrol Listesi detay UI ve oluşturma formları
