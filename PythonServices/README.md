# Python Services

Python servisleri Excel ve PowerPoint işlemleri için kullanılır.

## Kurulum

```bash
cd PythonServices
pip install -r requirements.txt
```

## Excel Service

### Özellikler

1. **Toplu Atama Import**
   - İç değerlendirici atamaları (BranchId, AssignedUserId, DueDate)
   - Dış müşteri atamaları (ExternalEmail, ExternalName, DueDate)

2. **Değerlendirme Sonuçları Export**
   - Tüm değerlendirme sonuçlarını Excel'e aktarma
   - Formatlı tablolar ve otomatik sütun genişliği

3. **Dashboard Verileri Export**
   - Çoklu sayfa (Overview, Branch Performance, Evaluator Performance, Trend Analysis)
   - Detaylı istatistikler ve metrikler

4. **Kontrol Listesi Şablonu Export**
   - Offline değerlendirme için Excel şablonu

### Kullanım

#### Python'dan

```python
from excel_service import ExcelService

service = ExcelService()

# Internal assignments import
assignments = service.parse_internal_assignments('assignments.xlsx')

# External assignments import
assignments = service.parse_external_assignments('external_assignments.xlsx')

# Export evaluations
service.export_evaluation_results(
    evaluations=evaluation_data,
    output_path='results.xlsx',
    project_name='Q1 2026'
)

# Export dashboard
service.export_dashboard_data(
    dashboard_data=dashboard_stats,
    output_path='dashboard.xlsx'
)
```

#### Komut Satırından

```bash
# Parse internal assignments
python excel_service.py parse_internal assignments.xlsx

# Parse external assignments
python excel_service.py parse_external external_assignments.xlsx

# Export evaluations
python excel_service.py export_evaluations data.json output.xlsx

# Export dashboard
python excel_service.py export_dashboard data.json dashboard.xlsx
```

### Excel Formatları

#### İç Atama (Internal Assignment)

| BranchId | AssignedUserId | DueDate |
|----------|---------------|---------|
| guid-1   | user-guid-1   | 2026-02-15 |
| guid-2   | user-guid-2   | 2026-02-20 |

#### Dış Atama (External Assignment)

| ExternalEmail | ExternalName | DueDate |
|--------------|--------------|---------|
| ahmet@example.com | Ahmet Yılmaz | 2026-02-15 |
| mehmet@example.com | Mehmet Demir | 2026-02-20 |

## PowerPoint Service

PowerPoint servisi otomatik rapor oluşturma için kullanılır.

### Özellikler

1. **Grafik Tabanlı Raporlar**
   - Trend grafikleri
   - Şube karşılaştırma grafikleri
   - Performans metrikleri

2. **Özelleştirilebilir Şablonlar**
   - Kurumsal şablon desteği
   - Logo ve renk özelleştirme

3. **Otomatik Veri Görselleştirme**
   - Bar charts, Line charts, Pie charts
   - Tablolar ve istatistikler

### Kullanım

Bakınız: `powerpoint_service.py`

## .NET API Entegrasyonu

Python servisleri .NET API tarafından subprocess veya HTTP API olarak kullanılabilir.

### Subprocess Kullanımı

```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "python",
        Arguments = $"excel_service.py parse_internal {filePath}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
    }
};

process.Start();
string output = process.StandardOutput.ReadToEnd();
process.WaitForExit();

var assignments = JsonSerializer.Deserialize<List<AssignmentDto>>(output);
```

### Flask API (Opsiyonel)

Flask ile REST API olarak sunmak için:

```python
from flask import Flask, request, jsonify
from excel_service import ExcelService

app = Flask(__name__)
service = ExcelService()

@app.route('/parse/internal', methods=['POST'])
def parse_internal():
    file = request.files['file']
    file.save('temp.xlsx')
    result = service.parse_internal_assignments('temp.xlsx')
    return jsonify(result)

if __name__ == '__main__':
    app.run(port=5001)
```

## Geliştirme

### Test

```bash
pytest tests/
```

### Yeni Özellik Ekleme

1. `excel_service.py` veya `powerpoint_service.py` dosyasına yeni metod ekleyin
2. Gerekli bağımlılıkları `requirements.txt`'e ekleyin
3. README'yi güncelleyin
4. Test yazın

## Bağımlılıklar

- **openpyxl**: Excel dosya işlemleri
- **pandas**: Veri manipülasyonu
- **python-pptx**: PowerPoint oluşturma
- **flask**: REST API (opsiyonel)
- **requests**: HTTP istekleri

## Lisans

MIT
