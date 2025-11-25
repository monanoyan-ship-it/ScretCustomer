# KnockoutJS Kullanımı - Gizli Müşteri Sistemi

Bu dokümanda, projemizde KnockoutJS'in nasıl kullanıldığı detaylı olarak açıklanmaktadır.

## İçindekiler
1. [KnockoutJS Nedir?](#knockoutjs-nedir)
2. [Projede Kullanım Mimarisi](#projede-kullanım-mimarisi)
3. [Observable'lar ve Computed Properties](#observablelar-ve-computed-properties)
4. [Data Binding Örnekleri](#data-binding-örnekleri)
5. [ViewModel Yapısı](#viewmodel-yapısı)
6. [Routing ve Sayfa Geçişleri](#routing-ve-sayfa-geçişleri)
7. [Servis Entegrasyonu](#servis-entegrasyonu)
8. [Best Practices](#best-practices)

---

## KnockoutJS Nedir?

KnockoutJS, **MVVM (Model-View-ViewModel)** pattern'ini uygulayan hafif bir JavaScript kütüphanesidir.

### Temel Özellikler:
- **Declarative Bindings**: HTML'de `data-bind` attribute'u ile veri bağlama
- **Automatic UI Refresh**: Veri değiştiğinde otomatik UI güncellemesi
- **Dependency Tracking**: Observable'lar arası bağımlılık yönetimi
- **Templating**: Dinamik HTML şablonları oluşturma

### Projede Kullanılan Versiyon:
```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/knockout/3.5.1/knockout-latest.js"></script>
```

---

## Projede Kullanım Mimarisi

### Dosya Yapısı

```
Frontend/wwwroot/
├── index.html                          # Ana HTML (SPA root)
├── js/
│   ├── app.js                          # Ana uygulama (Sammy.js routing)
│   ├── services/
│   │   ├── api.service.js              # HTTP request servisi
│   │   └── auth.service.js             # Authentication servisi
│   └── viewmodels/
│       ├── login.viewmodel.js          # Login sayfası ViewModel
│       ├── dashboard.viewmodel.js      # Dashboard ViewModel
│       └── checklist.viewmodel.js      # Checklist ViewModel
└── templates/
    ├── login.html                      # Login template
    ├── dashboard.html                  # Dashboard template
    └── checklists.html                 # Checklist template
```

### Mimari Akış

```
index.html (SPA container)
    ↓
app.js (Sammy.js routing)
    ↓
ViewModel'ler (KnockoutJS)
    ↓
Services (API & Auth)
    ↓
Backend API (.NET Core)
```

---

## Observable'lar ve Computed Properties

### 1. Observable (ko.observable)

**Tanım:** Değişiklik takibi yapan temel veri birimi.

**Örnek:**
```javascript
// ViewModel tanımı
function LoginViewModel() {
    var self = this;

    // Observable tanımlama
    self.username = ko.observable('');
    self.password = ko.observable('');
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');
}
```

**Kullanım:**
```javascript
// Değer okuma (parantez ile çağrı)
console.log(self.username());  // ''

// Değer yazma (parantez içinde değer)
self.username('admin');

// Değer değişti mi?
console.log(self.username());  // 'admin'
```

### 2. ObservableArray (ko.observableArray)

**Tanım:** Array değişikliklerini takip eden observable.

**Örnek:**
```javascript
function ChecklistViewModel() {
    var self = this;

    // ObservableArray tanımlama
    self.checklists = ko.observableArray([]);

    // Ekleme
    self.addChecklist = function(checklist) {
        self.checklists.push(checklist);
    };

    // Silme
    self.deleteChecklist = function(checklist) {
        self.checklists.remove(checklist);
    };

    // Tümünü değiştirme
    self.loadChecklists = function(data) {
        self.checklists(data);
    };
}
```

**Array Metotları:**
```javascript
self.checklists.push(item);           // Sona ekle
self.checklists.pop();                // Sondan çıkar
self.checklists.unshift(item);        // Başa ekle
self.checklists.shift();              // Baştan çıkar
self.checklists.remove(item);         // Spesifik item'ı sil
self.checklists.removeAll();          // Tümünü sil
self.checklists.splice(index, 1);     // Index'ten 1 item sil
```

### 3. Computed Observable (ko.computed)

**Tanım:** Diğer observable'lara bağımlı olan ve otomatik güncellenen değer.

**Örnek:**
```javascript
function DashboardViewModel() {
    var self = this;

    // Normal observable'lar
    self.averageScore = ko.observable(85.5);
    self.percentageChange = ko.observable(5.2);

    // Computed properties
    self.formattedAverageScore = ko.computed(function() {
        return self.averageScore().toFixed(1);  // "85.5"
    });

    self.percentageChangeClass = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'text-success' : 'text-danger';
    });

    self.percentageChangeIcon = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'bi-arrow-up' : 'bi-arrow-down';
    });

    // Role-based computed
    self.isAdmin = ko.computed(function() {
        return authService.isAdmin();
    });
}
```

**Otomatik Güncelleme:**
```javascript
// averageScore değiştiğinde formattedAverageScore otomatik güncellenir
self.averageScore(92.3);
console.log(self.formattedAverageScore());  // "92.3"
```

---

## Data Binding Örnekleri

### 1. Text Binding

**Kullanım:** Observable değerini HTML text olarak gösterir.

```html
<!-- Simple text binding -->
<span data-bind="text: username"></span>

<!-- Computed property -->
<h3 data-bind="text: formattedAverageScore"></h3>

<!-- Expression binding -->
<span data-bind="text: 'Hoş geldin, ' + user().fullName"></span>
```

### 2. Value Binding

**Kullanım:** Form input'larını observable'a bağlar (two-way binding).

```html
<!-- Text input -->
<input type="text" class="form-control" data-bind="value: username" placeholder="admin">

<!-- Password input -->
<input type="password" class="form-control" data-bind="value: password">

<!-- Number input -->
<input type="number" data-bind="value: score">

<!-- Textarea -->
<textarea data-bind="value: comments"></textarea>

<!-- Select -->
<select data-bind="value: selectedOption">
    <option value="1">Option 1</option>
    <option value="2">Option 2</option>
</select>
```

**İki Yönlü Bağlama:**
```javascript
// Kullanıcı input'a "admin" yazar
// → self.username() otomatik "admin" olur

// Kodda self.username('test') değiştirilir
// → Input'un value'su otomatik "test" olur
```

### 3. Visible Binding

**Kullanım:** Observable değerine göre element'i gösterir/gizler (display CSS).

```html
<!-- Loading spinner -->
<div data-bind="visible: isLoading" class="spinner-border"></div>

<!-- Error message -->
<div data-bind="visible: errorMessage" class="alert alert-danger">
    <span data-bind="text: errorMessage"></span>
</div>

<!-- Admin menu -->
<div data-bind="visible: isAdmin()">
    <a href="#/admin">Admin Panel</a>
</div>

<!-- Multiple conditions -->
<div data-bind="visible: !isLoading() && checklists().length > 0">
    <!-- Content -->
</div>
```

### 4. Click Binding

**Kullanım:** Click event'ini ViewModel fonksiyonuna bağlar.

```html
<!-- Simple click -->
<button data-bind="click: login">Giriş Yap</button>

<!-- Click with parameter -->
<button data-bind="click: deleteChecklist">Sil</button>

<!-- Click with inline function -->
<button data-bind="click: function() { viewChecklist(checklist) }">Görüntüle</button>

<!-- Click with bind (this context korunur) -->
<button data-bind="click: logout.bind($data)">Çıkış</button>
```

**ViewModel Fonksiyonu:**
```javascript
self.login = function() {
    self.isLoading(true);
    authService.login(self.username(), self.password())
        .then(function(user) {
            window.location.hash = '#/dashboard';
        })
        .catch(function(error) {
            self.errorMessage(error.message);
        })
        .finally(function() {
            self.isLoading(false);
        });
};
```

### 5. Submit Binding

**Kullanım:** Form submit event'ini bağlar (preventDefault otomatik).

```html
<form data-bind="submit: login">
    <input type="text" data-bind="value: username">
    <input type="password" data-bind="value: password">
    <button type="submit">Giriş</button>
</form>
```

### 6. Disable Binding

**Kullanım:** Observable değerine göre element'i disable eder.

```html
<!-- Loading sırasında disable -->
<button type="submit" data-bind="disable: isLoading">
    <span data-bind="visible: !isLoading()">Giriş Yap</span>
    <span data-bind="visible: isLoading">Giriş yapılıyor...</span>
</button>

<!-- Input disable -->
<input type="text" data-bind="value: username, disable: isLoading">
```

### 7. CSS Binding

**Kullanım:** Observable değerine göre CSS class'ları ekler/kaldırır.

```html
<!-- Simple CSS binding -->
<span data-bind="css: percentageChangeClass">
    +5%
</span>

<!-- Multiple classes -->
<span data-bind="css: {
    'text-success': percentageChange() >= 0,
    'text-danger': percentageChange() < 0,
    'fw-bold': isImportant()
}">
    Değişim: <span data-bind="text: percentageChange"></span>%
</span>

<!-- Icon binding -->
<i data-bind="css: percentageChangeIcon"></i>
```

### 8. ForEach Binding

**Kullanım:** ObservableArray'deki her item için template render eder.

```html
<!-- Simple foreach -->
<ul class="list-group" data-bind="foreach: checklists">
    <li class="list-group-item">
        <span data-bind="text: name"></span>
    </li>
</ul>

<!-- Foreach with actions -->
<ul class="list-group" data-bind="foreach: topBranches">
    <li class="list-group-item d-flex justify-content-between align-items-center">
        <span data-bind="text: branchName"></span>
        <span class="badge bg-success" data-bind="text: averageScore.toFixed(1)"></span>
    </li>
</ul>

<!-- Empty state -->
<div data-bind="visible: checklists().length === 0">
    Henüz kontrol listesi bulunmamaktadır.
</div>
```

**$data Context:**
```html
<div data-bind="foreach: items">
    <!-- $data: Current item -->
    <span data-bind="text: $data.name"></span>

    <!-- $parent: Parent ViewModel -->
    <button data-bind="click: $parent.deleteItem">Sil</button>

    <!-- $root: Root ViewModel -->
    <span data-bind="text: $root.appName"></span>
</div>
```

### 9. If / IfNot Binding

**Kullanım:** Condition'a göre element'i DOM'a ekler/çıkarır.

```html
<!-- If binding -->
<div data-bind="if: isAdmin()">
    <h3>Admin Panel</h3>
</div>

<!-- IfNot binding -->
<div data-bind="ifnot: isLoading">
    <p>İçerik yüklendi</p>
</div>

<!-- Nested conditions -->
<div data-bind="if: !isLoading()">
    <div data-bind="if: checklists().length > 0">
        <!-- List content -->
    </div>
    <div data-bind="ifnot: checklists().length > 0">
        <p>Liste boş</p>
    </div>
</div>
```

**Visible vs If:**
- `visible`: Element DOM'da kalır, sadece `display: none` olur
- `if`: Element DOM'dan tamamen kaldırılır

### 10. With Binding

**Kullanım:** Context'i spesifik bir observable'a değiştirir.

```html
<div data-bind="with: user">
    <!-- Context: user observable -->
    <span data-bind="text: fullName"></span>
    <span data-bind="text: role"></span>
</div>
```

---

## ViewModel Yapısı

### 1. Login ViewModel

**Dosya:** `js/viewmodels/login.viewmodel.js`

```javascript
function LoginViewModel() {
    var self = this;

    // Observable'lar
    self.username = ko.observable('');
    self.password = ko.observable('');
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Fonksiyonlar
    self.login = function() {
        self.errorMessage('');

        // Validation
        if (!self.username() || !self.password()) {
            self.errorMessage('Kullanıcı adı ve şifre gereklidir.');
            return;
        }

        self.isLoading(true);

        // API call
        authService.login(self.username(), self.password())
            .then(function(user) {
                console.log('Login successful:', user);
                window.location.hash = '#/dashboard';
            })
            .catch(function(error) {
                console.error('Login error:', error);
                self.errorMessage(error.message || 'Giriş başarısız.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };
}
```

**Template:** `templates/login.html`

```html
<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-5">
            <div class="card shadow">
                <div class="card-body p-5">
                    <h2>Gizli Müşteri Sistemi</h2>

                    <!-- Error Message -->
                    <div data-bind="visible: errorMessage" class="alert alert-danger">
                        <span data-bind="text: errorMessage"></span>
                    </div>

                    <!-- Login Form -->
                    <form data-bind="submit: login">
                        <div class="mb-3">
                            <label>Kullanıcı Adı</label>
                            <input type="text" class="form-control"
                                   data-bind="value: username, disable: isLoading"
                                   placeholder="admin" required>
                        </div>

                        <div class="mb-3">
                            <label>Şifre</label>
                            <input type="password" class="form-control"
                                   data-bind="value: password, disable: isLoading"
                                   placeholder="••••••••" required>
                        </div>

                        <button type="submit" class="btn btn-primary w-100"
                                data-bind="disable: isLoading">
                            <span data-bind="visible: !isLoading()">Giriş Yap</span>
                            <span data-bind="visible: isLoading">
                                <span class="spinner-border spinner-border-sm me-2"></span>
                                Giriş yapılıyor...
                            </span>
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
</div>
```

### 2. Dashboard ViewModel

**Dosya:** `js/viewmodels/dashboard.viewmodel.js`

```javascript
function DashboardViewModel() {
    var self = this;

    // Observable'lar
    self.user = ko.observable(authService.getUser());
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');

    // Stats
    self.totalEvaluations = ko.observable(0);
    self.averageScore = ko.observable(0);
    self.percentageChange = ko.observable(0);
    self.topBranches = ko.observableArray([]);
    self.bottomBranches = ko.observableArray([]);

    // Computed Properties
    self.isAdmin = ko.computed(function() {
        return authService.isAdmin();
    });

    self.formattedAverageScore = ko.computed(function() {
        return self.averageScore().toFixed(1);
    });

    self.percentageChangeClass = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'text-success' : 'text-danger';
    });

    self.percentageChangeIcon = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'bi-arrow-up' : 'bi-arrow-down';
    });

    // Fonksiyonlar
    self.loadDashboard = function() {
        self.isLoading(true);
        self.errorMessage('');

        var endpoint = self.isAdmin() ? '/dashboard/admin' : '/dashboard/representative';

        apiService.get(endpoint)
            .then(function(data) {
                if (self.isAdmin()) {
                    self.totalEvaluations(data.totalEvaluations || 0);
                    self.averageScore(data.averageScore || 0);
                    self.percentageChange(data.percentageChange || 0);
                    self.topBranches(data.topBranches || []);
                    self.bottomBranches(data.bottomBranches || []);
                }
            })
            .catch(function(error) {
                console.error('Dashboard error:', error);
                self.errorMessage('Dashboard verileri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.logout = function() {
        authService.logout();
        window.location.hash = '#/login';
    };

    // Initialize
    self.loadDashboard();
}
```

### 3. Checklist ViewModel

**Dosya:** `js/viewmodels/checklist.viewmodel.js`

```javascript
function ChecklistViewModel() {
    var self = this;

    // Observable'lar
    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');

    // Fonksiyonlar
    self.loadChecklists = function() {
        self.isLoading(true);
        self.errorMessage('');

        apiService.get('/checklists')
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Checklists error:', error);
                self.errorMessage('Kontrol listeleri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.viewChecklist = function(checklist) {
        window.location.hash = '#/checklists/' + checklist.id;
    };

    self.deleteChecklist = function(checklist) {
        if (!confirm('Bu kontrol listesini silmek istediğinizden emin misiniz?')) {
            return;
        }

        apiService.delete('/checklists/' + checklist.id)
            .then(function() {
                self.checklists.remove(checklist);
                alert('Kontrol listesi başarıyla silindi.');
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                alert('Kontrol listesi silinirken bir hata oluştu.');
            });
    };

    // Initialize
    self.loadChecklists();
}
```

---

## Routing ve Sayfa Geçişleri

### Sammy.js Routing

**Dosya:** `js/app.js`

```javascript
(function() {
    'use strict';

    var app = Sammy('#app-container', function() {
        var self = this;

        // Helper: Authentication check
        function requireAuth() {
            if (!authService.isAuthenticated()) {
                self.redirect('#/login');
                return false;
            }
            return true;
        }

        // Helper: Apply ViewModel bindings
        function applyViewModel(viewModel, templateUrl) {
            fetch(templateUrl)
                .then(res => res.text())
                .then(html => {
                    const container = document.getElementById('app-container');
                    ko.cleanNode(container);  // Clean old bindings
                    container.innerHTML = html;
                    ko.applyBindings(viewModel, container);  // Apply new bindings
                })
                .catch(err => {
                    console.error('Error loading template:', err);
                    alert('Sayfa yüklenirken bir hata oluştu.');
                });
        }

        // Routes
        this.get('#/login', function() {
            if (authService.isAuthenticated()) {
                this.redirect('#/dashboard');
                return;
            }
            applyViewModel(new LoginViewModel(), '/templates/login.html');
        });

        this.get('#/dashboard', function() {
            if (!requireAuth()) return;
            applyViewModel(new DashboardViewModel(), '/templates/dashboard.html');
        });

        this.get('#/checklists', function() {
            if (!requireAuth()) return;
            if (!authService.isAdmin()) {
                alert('Bu sayfaya erişim yetkiniz yok.');
                self.redirect('#/dashboard');
                return;
            }
            applyViewModel(new ChecklistViewModel(), '/templates/checklists.html');
        });

        // Default route
        this.get('/', function() {
            if (authService.isAuthenticated()) {
                this.redirect('#/dashboard');
            } else {
                this.redirect('#/login');
            }
        });
    });

    // Start app when DOM ready
    $(document).ready(function() {
        app.run('#/');
    });
})();
```

### ko.cleanNode() ve ko.applyBindings()

**ko.cleanNode():**
- Mevcut KnockoutJS binding'lerini temizler
- Memory leak'i önler
- Yeni ViewModel bind etmeden önce çağrılmalı

**ko.applyBindings():**
- ViewModel'i HTML template'e bağlar
- İki parametre alır: `(viewModel, domElement)`
- Sayfa başına bir kez çağrılmalı

**Örnek:**
```javascript
const container = document.getElementById('app-container');

// Eski binding'leri temizle
ko.cleanNode(container);

// Yeni ViewModel'i bind et
ko.applyBindings(new DashboardViewModel(), container);
```

---

## Servis Entegrasyonu

### 1. API Service

**Dosya:** `js/services/api.service.js`

```javascript
var apiService = (function() {
    'use strict';

    const BASE_URL = 'https://localhost:7001/api';

    function getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = 'Bearer ' + token;
            }
        }

        return headers;
    }

    function handleResponse(response) {
        if (!response.ok) {
            if (response.status === 401) {
                // Unauthorized - redirect to login
                authService.logout();
                window.location.hash = '#/login';
            }
            return response.json().then(err => Promise.reject(err));
        }

        if (response.status === 204) {
            return Promise.resolve();
        }

        return response.json();
    }

    return {
        get: function(endpoint) {
            return fetch(BASE_URL + endpoint, {
                method: 'GET',
                headers: getHeaders()
            }).then(handleResponse);
        },

        post: function(endpoint, data, includeAuth = true) {
            return fetch(BASE_URL + endpoint, {
                method: 'POST',
                headers: getHeaders(includeAuth),
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        put: function(endpoint, data) {
            return fetch(BASE_URL + endpoint, {
                method: 'PUT',
                headers: getHeaders(),
                body: JSON.stringify(data)
            }).then(handleResponse);
        },

        delete: function(endpoint) {
            return fetch(BASE_URL + endpoint, {
                method: 'DELETE',
                headers: getHeaders()
            }).then(handleResponse);
        }
    };
})();
```

### 2. Auth Service

**Dosya:** `js/services/auth.service.js`

```javascript
var authService = (function() {
    'use strict';

    const TOKEN_KEY = 'jwt_token';
    const USER_KEY = 'user';

    return {
        login: function(username, password) {
            return apiService.post('/auth/login', { username, password }, false)
                .then(function(data) {
                    localStorage.setItem(TOKEN_KEY, data.token);
                    localStorage.setItem(USER_KEY, JSON.stringify(data.user));
                    return data.user;
                });
        },

        logout: function() {
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(USER_KEY);
        },

        getToken: function() {
            return localStorage.getItem(TOKEN_KEY);
        },

        getUser: function() {
            const userStr = localStorage.getItem(USER_KEY);
            return userStr ? JSON.parse(userStr) : null;
        },

        isAuthenticated: function() {
            return !!this.getToken();
        },

        hasRole: function(role) {
            const user = this.getUser();
            return user && user.role === role;
        },

        isAdmin: function() {
            return this.hasRole('Admin');
        },

        isTeamLeader: function() {
            return this.hasRole('TeamLeader');
        },

        isEvaluator: function() {
            return this.hasRole('Evaluator');
        }
    };
})();
```

### ViewModel'de Servis Kullanımı

```javascript
function MyViewModel() {
    var self = this;

    self.data = ko.observableArray([]);
    self.isLoading = ko.observable(false);

    // GET request
    self.loadData = function() {
        self.isLoading(true);

        apiService.get('/endpoint')
            .then(function(response) {
                self.data(response);  // ObservableArray'e veri yükle
            })
            .catch(function(error) {
                console.error('Error:', error);
                alert('Veri yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // POST request
    self.saveItem = function(item) {
        apiService.post('/endpoint', item)
            .then(function(response) {
                self.data.push(response);  // Array'e ekle
                alert('Kayıt başarılı!');
            })
            .catch(function(error) {
                console.error('Save error:', error);
                alert('Kayıt başarısız!');
            });
    };

    // DELETE request
    self.deleteItem = function(item) {
        if (!confirm('Silmek istediğinizden emin misiniz?')) return;

        apiService.delete('/endpoint/' + item.id)
            .then(function() {
                self.data.remove(item);  // Array'den çıkar
                alert('Silme başarılı!');
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                alert('Silme başarısız!');
            });
    };
}
```

---

## Best Practices

### 1. ViewModel Yapısı

**✅ İyi:**
```javascript
function MyViewModel() {
    var self = this;  // 'this' context'ini sakla

    // Observable'lar en üstte
    self.data = ko.observable('');
    self.items = ko.observableArray([]);

    // Computed properties
    self.hasItems = ko.computed(function() {
        return self.items().length > 0;
    });

    // Fonksiyonlar
    self.loadData = function() { /*...*/ };

    // Initialize
    self.loadData();
}
```

**❌ Kötü:**
```javascript
function MyViewModel() {
    // 'this' kullanımı - context kaybı riski
    this.data = ko.observable('');

    this.loadData = function() {
        // 'this' burada farklı olabilir!
        console.log(this.data());
    };
}
```

### 2. Observable Kullanımı

**✅ İyi:**
```javascript
// Observable'a parantez ile eriş
var username = self.username();

// Observable'ı güncelle
self.username('admin');

// ObservableArray'e ekle
self.items.push(newItem);
```

**❌ Kötü:**
```javascript
// Parantez unutma hatası
var username = self.username;  // Function döner, değer değil!

// Direct assignment (observable'ı yok eder!)
self.username = 'admin';  // Observable artık çalışmaz!
```

### 3. Computed Dependency

**✅ İyi:**
```javascript
self.fullName = ko.computed(function() {
    // Dependency açık ve net
    return self.firstName() + ' ' + self.lastName();
});
```

**❌ Kötü:**
```javascript
self.fullName = ko.computed(function() {
    // Dependency tracking çalışmaz!
    var first = self.firstName();  // Local variable
    var last = self.lastName();
    return first + ' ' + last;  // Sadece ilk çağrıda çalışır
});
```

### 4. Memory Leak Önleme

**✅ İyi:**
```javascript
// Sayfa değişikliğinde binding'leri temizle
function applyViewModel(viewModel, templateUrl) {
    const container = document.getElementById('app-container');
    ko.cleanNode(container);  // Eski binding'leri temizle
    container.innerHTML = html;
    ko.applyBindings(viewModel, container);
}
```

### 5. Async İşlemlerde Loading State

**✅ İyi:**
```javascript
self.loadData = function() {
    self.isLoading(true);
    self.errorMessage('');

    apiService.get('/endpoint')
        .then(function(data) {
            self.data(data);
        })
        .catch(function(error) {
            self.errorMessage('Hata oluştu!');
        })
        .finally(function() {
            self.isLoading(false);  // Her durumda kapan
        });
};
```

### 6. Template'de Complex Logic Yok

**✅ İyi:**
```javascript
// ViewModel'de computed property
self.hasCompletedItems = ko.computed(function() {
    return self.items().filter(x => x.completed).length > 0;
});
```
```html
<div data-bind="visible: hasCompletedItems">
    Tamamlanmış görevler var!
</div>
```

**❌ Kötü:**
```html
<!-- Template'de complex logic -->
<div data-bind="visible: items().filter(x => x.completed).length > 0">
    Tamamlanmış görevler var!
</div>
```

### 7. Context Binding ($parent, $root)

**Nested foreach'te parent'e erişim:**
```html
<div data-bind="foreach: sections">
    <h3 data-bind="text: title"></h3>

    <div data-bind="foreach: questions">
        <p data-bind="text: questionText"></p>

        <!-- Parent ViewModel'deki fonksiyonu çağır -->
        <button data-bind="click: $parent.deleteQuestion">Sil</button>

        <!-- Root ViewModel'e eriş -->
        <span data-bind="text: $root.appName"></span>
    </div>
</div>
```

### 8. Form Validation

```javascript
self.save = function() {
    // Client-side validation
    if (!self.name() || self.name().trim() === '') {
        self.errorMessage('İsim gereklidir!');
        return;
    }

    if (self.age() < 18) {
        self.errorMessage('Yaş 18\'den küçük olamaz!');
        return;
    }

    // Validation başarılı, API call
    apiService.post('/save', {
        name: self.name(),
        age: self.age()
    });
};
```

### 9. Dispose Pattern (Cleanup)

```javascript
function MyViewModel() {
    var self = this;

    self.data = ko.observable('');

    // Computed observable (dispose gerektirir)
    self.upperData = ko.computed(function() {
        return self.data().toUpperCase();
    });

    // Cleanup fonksiyonu
    self.dispose = function() {
        self.upperData.dispose();  // Computed'ı temizle
    };
}

// Sayfa değiştiğinde
var vm = new MyViewModel();
// ...
vm.dispose();  // Cleanup
ko.cleanNode(container);
```

---

## Sık Karşılaşılan Hatalar

### 1. "Cannot read property of undefined"

**Hata:**
```javascript
self.user = ko.observable(null);
// ...
<span data-bind="text: user.fullName"></span>  // ERROR!
```

**Çözüm:**
```javascript
// Null check
<span data-bind="text: user() ? user().fullName : ''"></span>

// Veya with binding
<div data-bind="with: user">
    <span data-bind="text: fullName"></span>
</div>
```

### 2. Observable Parantez Unutma

**Hata:**
```javascript
if (self.isLoading) {  // Function'u kontrol ediyor (her zaman true!)
    console.log('Loading...');
}
```

**Çözüm:**
```javascript
if (self.isLoading()) {  // Observable'ın değerini al
    console.log('Loading...');
}
```

### 3. ObservableArray Direct Assignment

**Hata:**
```javascript
self.items = [1, 2, 3];  // Observable yok oldu!
```

**Çözüm:**
```javascript
self.items([1, 2, 3]);  // Observable'ı güncelle
```

### 4. Multiple applyBindings

**Hata:**
```javascript
ko.applyBindings(vm, container);
ko.applyBindings(vm, container);  // ERROR: Already bound!
```

**Çözüm:**
```javascript
ko.cleanNode(container);  // Önce temizle
ko.applyBindings(vm, container);  // Sonra bind et
```

---

## Örnek Kompleks ViewModel

```javascript
function ChecklistEditorViewModel(checklistId) {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Data
    self.checklist = ko.observable({
        name: ko.observable(''),
        description: ko.observable(''),
        sections: ko.observableArray([])
    });

    // Computed
    self.totalQuestions = ko.computed(function() {
        var total = 0;
        self.checklist().sections().forEach(function(section) {
            total += section.questions().length;
        });
        return total;
    });

    self.isValid = ko.computed(function() {
        var name = self.checklist().name();
        var sections = self.checklist().sections();
        return name && name.trim() !== '' && sections.length > 0;
    });

    // Actions
    self.addSection = function() {
        self.checklist().sections.push({
            title: ko.observable(''),
            questions: ko.observableArray([])
        });
    };

    self.removeSection = function(section) {
        self.checklist().sections.remove(section);
    };

    self.addQuestion = function(section) {
        section.questions.push({
            text: ko.observable(''),
            type: ko.observable('YesNo'),
            weight: ko.observable(10)
        });
    };

    self.save = function() {
        if (!self.isValid()) {
            alert('Lütfen tüm alanları doldurun!');
            return;
        }

        self.isSaving(true);
        self.errorMessage('');

        // DTO preparation
        var dto = {
            name: self.checklist().name(),
            description: self.checklist().description(),
            sections: self.checklist().sections().map(function(section) {
                return {
                    title: section.title(),
                    questions: section.questions().map(function(q) {
                        return {
                            text: q.text(),
                            type: q.type(),
                            weight: q.weight()
                        };
                    })
                };
            })
        };

        apiService.put('/checklists/' + checklistId, dto)
            .then(function() {
                alert('Kayıt başarılı!');
                window.location.hash = '#/checklists';
            })
            .catch(function(error) {
                self.errorMessage('Kayıt başarısız: ' + error.message);
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Load existing checklist
    self.load = function() {
        self.isLoading(true);

        apiService.get('/checklists/' + checklistId)
            .then(function(data) {
                // Convert API data to observables
                self.checklist({
                    name: ko.observable(data.name),
                    description: ko.observable(data.description),
                    sections: ko.observableArray(data.sections.map(function(section) {
                        return {
                            title: ko.observable(section.title),
                            questions: ko.observableArray(section.questions.map(function(q) {
                                return {
                                    text: ko.observable(q.text),
                                    type: ko.observable(q.type),
                                    weight: ko.observable(q.weight)
                                };
                            }))
                        };
                    }))
                });
            })
            .catch(function(error) {
                self.errorMessage('Yükleme başarısız!');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Cleanup
    self.dispose = function() {
        self.totalQuestions.dispose();
        self.isValid.dispose();
    };

    // Initialize
    if (checklistId) {
        self.load();
    } else {
        self.isLoading(false);
    }
}
```

---

## Özet

### KnockoutJS Kullanımımız:

1. **MVVM Pattern**: View (HTML) ve ViewModel (JavaScript) ayrımı
2. **Declarative Bindings**: `data-bind` ile UI-data bağlama
3. **Observables**: Otomatik UI güncellemesi
4. **Computed Properties**: Türetilmiş değerler
5. **Sammy.js Routing**: SPA sayfa geçişleri
6. **Service Layer**: API ve Auth servisleri
7. **Template System**: HTML template dosyaları

### Avantajlar:
- ✅ Hafif ve hızlı (sadece 60KB)
- ✅ Vanilla JavaScript ile uyumlu
- ✅ jQuery ile entegre
- ✅ Öğrenmesi kolay
- ✅ Two-way binding

### Dezavantajlar:
- ❌ Modern framework'ler kadar güçlü değil
- ❌ Component sistemi yok
- ❌ Virtual DOM yok
- ❌ Büyük projelerde karmaşıklaşabilir

---

**Dokümantasyon Tarihi:** 2025-01-24
**Versiyon:** 1.0
**KnockoutJS Versiyon:** 3.5.1
