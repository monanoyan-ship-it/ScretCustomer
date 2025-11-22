# Gizli Müşteri Değerlendirme Sistemi

Kapsamlı bir gizli müşteri (secret customer) değerlendirme ve raporlama sistemi.

## 📋 Özellikler

### Backend (.NET Core 9.0)
- ✅ Clean Architecture (Core, Data, Services, API katmanları)
- ✅ PostgreSQL database ile EF Core
- ✅ JWT authentication & authorization
- ✅ Role-based access control (Admin, TeamLeader, Evaluator, CustomerRepresentative)
- ✅ RESTful API endpoints
- ✅ Repository Pattern
- ✅ DTOs ve AutoMapper kullanımı
- ✅ BCrypt password hashing

### Frontend (KnockoutJS + Bootstrap 5)
- ✅ Single Page Application (SPA)
- ✅ MVVM pattern ile KnockoutJS 3.5.1
- ✅ Bootstrap 5.3.0 responsive UI
- ✅ JWT token yönetimi
- ✅ Role-based navigation
- ✅ Dinamik formlar
- ✅ Real-time progress tracking

### Modüller
1. **Authentication & Authorization** - JWT tabanlı kimlik doğrulama
2. **Kontrol Listeleri** - Dinamik soru formları (4 soru tipi: YesNo, Rating, Text, MultipleChoice)
3. **Projeler** - Değerlendirme projeleri yönetimi
4. **Atamalar** - İç/Dış değerlendirici atamaları
5. **Değerlendirmeler** - Form doldurma ve N/A desteği
6. **Dashboard** - Role-based istatistikler ve raporlar

## 🚀 Hızlı Başlangıç

### Gereksinimler
- .NET 9.0 SDK
- PostgreSQL 15+
- Node.js (optional, for future enhancements)

### Backend Kurulumu

1. **Clone Repository**
```bash
git clone https://github.com/monanoyan-ship-it/ScretCustomer.git
cd ScretCustomer
```

2. **Database Oluştur**
```sql
CREATE DATABASE SecretCustomerDB;
```

3. **Connection String Ayarla**
`Backend/SecretCustomer.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SecretCustomerDB;Username=postgres;Password=your_password"
  }
}
```

4. **NuGet Paketlerini Yükle**
```bash
cd Backend/SecretCustomer.API
dotnet restore
```

5. **Migration Çalıştır**
```bash
dotnet ef database update --startup-project Backend/SecretCustomer.API
```

6. **API'yi Çalıştır**
```bash
dotnet run
```

API `https://localhost:7001` adresinde çalışacak.

### Frontend Kurulumu

1. **Static Files Serve**
Frontend dosyaları `Frontend/wwwroot/` klasöründe.

2. **API URL Ayarla**
`Frontend/wwwroot/js/services/api.js`:
```javascript
const BASE_URL = 'https://localhost:7001/api';
```

3. **Tarayıcıda Aç**
`Frontend/wwwroot/index.html` dosyasını bir web server ile serve edin veya .NET API ile birlikte kullanın.

## 👤 Test Kullanıcıları

Seed data ile oluşturulan test kullanıcıları:

| Kullanıcı Adı | Şifre | Rol |
|---------------|-------|-----|
| admin | Admin@123 | Admin |
| teamleader | Leader@123 | TeamLeader |
| evaluator1 | Eval@123 | Evaluator |
| evaluator2 | Eval@123 | Evaluator |

## 📁 Proje Yapısı

```
ScretCustomer/
├── Backend/
│   ├── SecretCustomer.API/          # Web API (Controllers, Program.cs)
│   ├── SecretCustomer.Core/         # Domain entities, DTOs, Interfaces
│   ├── SecretCustomer.Data/         # EF Core, Repositories, Configurations
│   └── SecretCustomer.Services/     # Business logic, Services
├── Frontend/
│   └── wwwroot/
│       ├── index.html               # SPA ana sayfa
│       ├── css/app.css              # Özel stiller
│       └── js/
│           ├── app.js               # Ana ViewModel + Routing
│           ├── services/            # API ve Auth servisleri
│           └── viewmodels/          # Sayfa ViewModels
├── PythonServices/                  # Excel/PowerPoint işlemleri (TODO)
└── docs/                            # 11 adet MD dokümantasyon
```

## 🗄️ Database Schema

### Entities (9 tablo)
- **Users**: Sistem kullanıcıları
- **Branches**: Şubeler
- **Checklists**: Kontrol listeleri
- **Sections**: Kontrol listesi bölümleri
- **Questions**: Sorular (4 tip destekli)
- **Projects**: Değerlendirme projeleri
- **Assignments**: Atamalar (Internal/External)
- **Evaluations**: Tamamlanmış değerlendirmeler
- **Answers**: Soru cevapları

### İlişkiler
```
Checklist 1--* Section 1--* Question
Project 1--* Assignment *--1 Branch
Assignment *--1 User (Evaluator)
Assignment 1--1 Evaluation 1--* Answer *--1 Question
```

## 🔐 Authentication & Authorization

- **JWT Bearer Token** authentication
- **BCrypt** password hashing
- **Role-based** access control
- Token expiration: 24 saat (configurable)

### API Endpoints

#### Auth
- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/register` - Yeni kullanıcı kaydı

#### Checklists
- `GET /api/checklist` - Tüm kontrol listeleri
- `GET /api/checklist/{id}` - Detay
- `POST /api/checklist` - Yeni oluştur
- `PUT /api/checklist/{id}` - Güncelle
- `POST /api/checklist/{id}/clone` - Klonla

#### Assignments
- `GET /api/assignment` - Tüm atamalar
- `GET /api/assignment/evaluator/{id}` - Değerlendiriciye ait
- `POST /api/assignment` - Tekil atama
- `POST /api/assignment/bulk` - Toplu atama
- `DELETE /api/assignment/{id}` - Sil

#### Evaluations
- `POST /api/evaluation/submit` - Değerlendirme gönder

#### Dashboard
- `GET /api/dashboard/admin` - Admin dashboard
- `GET /api/dashboard/teamleader/{id}` - Team leader dashboard
- `GET /api/dashboard/representative/{branchId}` - Representative dashboard

## 📊 Özellik Detayları

### N/A (Not Applicable) Özelliği
Soruların "N/A" işaretlenmesine izin verir. N/A işaretlendiğinde:
- Soru puanı toplam puandan çıkarılır
- Kalan sorulara göre yüzde hesaplanır
- Orijinal ağırlık oranları korunur

**Örnek:**
```
Toplam Puan: 100
N/A Sorular: 20 puan
Kalan: 80 puan
Alınan: 60 puan
Yüzde: (60/80) * 100 = 75%
```

### Soru Tipleri

1. **YesNo**: Evet/Hayır soruları (radio button)
2. **Rating**: 1-5 arası puanlama (radio button group)
3. **Text**: Açık uçlu metin cevabı (textarea)
4. **MultipleChoice**: Çoktan seçmeli (dropdown)

### Atama Tipleri

1. **Internal**: İç değerlendirici ataması
   - Değerlendirici kullanıcı seçilir
   - Sistem üzerinden form doldurulur

2. **External**: Dış değerlendirme (müşteri anketi)
   - Unique link oluşturulur
   - Link paylaşılarak değerlendirme toplanır

## 📚 Dokümantasyon

Detaylı dokümantasyon `docs/` klasöründe:

1. **01-proje-kurulumu.md** - Proje kurulum rehberi
2. **02-database-modeli.md** - Database şeması ve ilişkiler
3. **03-kontrol-listesi-modulu.md** - Checklist modülü detayları
4. **04-atama-mantigi-modulu.md** - Assignment mantığı
5. **05-dashboard-ve-raporlama.md** - Dashboard ve raporlama
6. **06-authentication-authorization.md** - Auth sistemi
7. **07-frontend-temel-yapi.md** - Frontend mimarisi
8. **08-frontend-kontrol-listesi-ui.md** - Checklist UI detayları
9. **09-frontend-degerlendirme-formu-ui.md** - Evaluation form UI
10. **10-frontend-ozet.md** - Frontend genel özet
11. **11-database-setup.md** - Database setup rehberi

## 🔧 Teknoloji Stack

### Backend
- .NET Core 9.0
- Entity Framework Core 9.0
- PostgreSQL (Npgsql 9.0.2)
- JWT Bearer 9.0.0
- BCrypt.Net-Next 4.0.3

### Frontend
- KnockoutJS 3.5.1
- Bootstrap 5.3.0
- Chart.js 4.4.0
- Vanilla JavaScript (ES6+)

### Future
- Python 3.10+ (Excel/PowerPoint işlemleri için)
- openpyxl, python-pptx

## 🚧 Yapılacaklar (Roadmap)

- [ ] Python servisleri (Excel import/export, PowerPoint raporları)
- [ ] Projects management UI
- [ ] Dashboard charts (Chart.js entegrasyonu)
- [ ] Email notifications
- [ ] PDF export
- [ ] Advanced filtering & search
- [ ] Audit logs
- [ ] User management UI
- [ ] Dark mode
- [ ] Mobile app (React Native / Flutter)

## 🤝 Katkıda Bulunma

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'feat: Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

Bu proje MIT lisansı altında lisanslanmıştır.

## 👨‍💻 Geliştirici

**Claude Code** tarafından geliştirilmiştir.

## 📞 İletişim

Sorularınız için issue açabilir veya pull request gönderebilirsiniz.

---

**🤖 Generated with [Claude Code](https://claude.com/claude-code)**
