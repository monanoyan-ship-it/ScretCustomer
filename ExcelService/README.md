# Excel Service

Excel import/export islemleri icin Python FastAPI mikroservisi.

## Hizli Baslangic

### 1. Servisi Baslat

```bash
# Windows
run_service.bat

# veya manuel olarak
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

Servis `http://localhost:8002` adresinde calisacak.

### 2. API Dokumantasyonu

Servis calisirken asagidaki adresten interaktif API dokumantasyonuna ulasabilirsiniz:

- Swagger UI: http://localhost:8002/docs
- ReDoc: http://localhost:8002/redoc

### 3. Testleri Calistir

```bash
# Servis calisirken baska bir terminal'de
run_tests.bat
```

## API Endpointleri

### Health Check
```
GET /health
```

### Template Olustur
```
POST /generate-template
Content-Type: application/json

{
    "name": "Kullanici Listesi",
    "description": "Kullanici import sablonu",
    "entity_type": "User",
    "sheet_name": "Kullanicilar",
    "has_header": true,
    "columns": [
        {
            "column_name": "Ad",
            "property_name": "firstName",
            "column_type": "Text",
            "order": 1,
            "is_required": true
        }
    ]
}
```

Desteklenen kolon tipleri:
- `Text` - Metin
- `Number` - Sayi
- `Date` - Tarih (YYYY-MM-DD, DD/MM/YYYY, DD.MM.YYYY)
- `Boolean` - true/false, evet/hayir, 1/0
- `Email` - E-posta adresi
- `Phone` - Telefon numarasi
- `Dropdown` - Secenekli liste

### Excel Parse Et
```
POST /parse-excel
Content-Type: multipart/form-data

file: Excel dosyasi (.xlsx)
template: JSON formatinda template (form field olarak)
```

## C# Entegrasyonu

```csharp
// Program.cs veya Startup.cs
builder.Services.AddHttpClient("ExcelProcessor", client =>
{
    client.BaseAddress = new Uri("http://localhost:8002");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Service icinde kullanim
public class ExcelService
{
    private readonly HttpClient _client;

    public ExcelService(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient("ExcelProcessor");
    }

    public async Task<byte[]> GenerateTemplateAsync(object template)
    {
        var response = await _client.PostAsJsonAsync("/generate-template", template);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
```

## Dosya Yapisi

```
ExcelService/
├── main.py              # FastAPI uygulama
├── models.py            # Pydantic modelleri
├── excel_handler.py     # Excel islemleri
├── requirements.txt     # Python bagimliliklari
├── test_service.py      # Test scripti
├── run_service.bat      # Servisi baslatma (Windows)
├── run_tests.bat        # Testleri calistirma (Windows)
└── README.md            # Bu dosya
```
