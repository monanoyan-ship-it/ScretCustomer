# Gizli Müşteri Değerlendirme Sistemi - Proje Kurulumu

## Proje Özeti
Bu sistem, kurumların çağrı merkezi/şube/saha bölümlerine yapılan denetim veya gizli müşteri ziyaretlerini ve memnuniyet anketlerini tek bir dijital platformdan toplamak için geliştirilmiştir.

## Teknoloji Stack
- **Backend**: .NET Core 9.0 Web API
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 9.0 + Npgsql
- **Python Servisleri**: Excel ve PowerPoint işlemleri için
- **Frontend**: KnockoutJS + Bootstrap + Chart.js

## Proje Yapısı

```
SecretCustomer/
├── Backend/
│   ├── SecretCustomer.API/          # Web API projesi
│   ├── SecretCustomer.Core/         # Domain modelleri ve interface'ler
│   ├── SecretCustomer.Data/         # EF Core, DbContext, Migrations
│   └── SecretCustomer.Services/     # Business Logic katmanı
├── PythonServices/
│   ├── excel_service.py             # Excel import/export işlemleri
│   ├── powerpoint_service.py        # PowerPoint raporlama
│   └── requirements.txt             # Python bağımlılıkları
├── Frontend/
│   └── wwwroot/
│       ├── css/                     # Bootstrap + Custom CSS
│       ├── js/
│       │   ├── viewmodels/          # KnockoutJS ViewModels
│       │   ├── components/          # Reusable Components
│       │   └── services/            # API Service çağrıları
│       └── index.html
└── docs/                            # Dokümantasyon
```

## Kurulum Adımları

### 1. Solution Oluşturma
```bash
dotnet new sln -n SecretCustomer
```

### 2. Proje Klasör Yapısı
```bash
mkdir -p Backend/SecretCustomer.API Backend/SecretCustomer.Core Backend/SecretCustomer.Data Backend/SecretCustomer.Services PythonServices Frontend/wwwroot/{css,js/viewmodels,js/components,js/services}
```

### 3. .NET Projeleri Oluşturma
```bash
# Web API projesi
cd Backend/SecretCustomer.API
dotnet new webapi --use-controllers
cd ../..

# Class Library projeleri
cd Backend/SecretCustomer.Core
dotnet new classlib
cd ../SecretCustomer.Data
dotnet new classlib
cd ../SecretCustomer.Services
dotnet new classlib
cd ../..
```

### 4. Projeleri Solution'a Ekleme
```bash
dotnet sln add Backend/SecretCustomer.API/SecretCustomer.API.csproj
dotnet sln add Backend/SecretCustomer.Core/SecretCustomer.Core.csproj
dotnet sln add Backend/SecretCustomer.Data/SecretCustomer.Data.csproj
dotnet sln add Backend/SecretCustomer.Services/SecretCustomer.Services.csproj
```

### 5. Proje Referansları
```bash
# API -> Core ve Services
dotnet add Backend/SecretCustomer.API/SecretCustomer.API.csproj reference Backend/SecretCustomer.Core/SecretCustomer.Core.csproj
dotnet add Backend/SecretCustomer.API/SecretCustomer.API.csproj reference Backend/SecretCustomer.Services/SecretCustomer.Services.csproj

# Data -> Core
dotnet add Backend/SecretCustomer.Data/SecretCustomer.Data.csproj reference Backend/SecretCustomer.Core/SecretCustomer.Core.csproj

# Services -> Core ve Data
dotnet add Backend/SecretCustomer.Services/SecretCustomer.Services.csproj reference Backend/SecretCustomer.Core/SecretCustomer.Core.csproj
dotnet add Backend/SecretCustomer.Services/SecretCustomer.Services.csproj reference Backend/SecretCustomer.Data/SecretCustomer.Data.csproj
```

### 6. NuGet Paketleri

#### PostgreSQL ve Entity Framework Core (Data projesi)
```bash
dotnet add Backend/SecretCustomer.Data/SecretCustomer.Data.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.2
dotnet add Backend/SecretCustomer.Data/SecretCustomer.Data.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.0
```

#### EF Core Tools (API projesi)
```bash
dotnet add Backend/SecretCustomer.API/SecretCustomer.API.csproj package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
```

## Mimari Katmanlar

### 1. SecretCustomer.Core
- Domain entity'leri
- Interface'ler (IRepository, IService)
- DTOs (Data Transfer Objects)
- Enums ve Constants

### 2. SecretCustomer.Data
- DbContext (ApplicationDbContext)
- Entity Configurations
- Migrations
- Repository implementasyonları

### 3. SecretCustomer.Services
- Business Logic
- Service implementasyonları
- Validasyon kuralları
- N/A puan yeniden dağıtım algoritması

### 4. SecretCustomer.API
- Controllers
- Middleware'ler
- Dependency Injection configuration
- Python servis entegrasyonu

## Ana Modüller

### 1. Kontrol Listesi Modülü
- Dinamik form builder
- Soru tipleri: Çoktan seçmeli, Likert, Yıldızlı değerlendirme, Metin alanı
- Bölüm bazlı form yapısı
- Otomatik puan hesaplama
- N/A seçeneği ve akıllı puan yeniden dağıtımı

### 2. Atama Mantığı Modülü
- İç değerlendirici atamaları (Excel import desteği)
- Dış müşteri anket linkleri
- Tek kullanım kontrolü
- Proje bazlı atama sistemi

### 3. Dashboard ve Raporlama Modülü
- Role-based dashboardlar:
  - **Yönetici**: Tüm veriler, şube karşılaştırmaları
  - **Takım Lideri**: Kendi şubesi
  - **Müşteri Temsilcisi**: Kendi değerlendirmeleri
- Grafikler: Trend grafiği, Şube karşılaştırma, Performans metrikleri
- Export: Excel (veri), PowerPoint (grafikli raporlar)

## Veritabanı Yapılandırması
PostgreSQL kullanılacak. ConnectionString appsettings.json içinde tanımlanacak:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=SecretCustomerDB;Username=postgres;Password=yourpassword"
  }
}
```

## Sonraki Adımlar
1. Database modelleri oluşturma
2. DbContext yapılandırması
3. Authentication ve Authorization sistemi
4. Kontrol Listesi API'leri
5. Python servisleri entegrasyonu
6. Frontend geliştirme

---
**Oluşturulma Tarihi**: 2025-11-22
**Versiyon**: 1.0
