# PDF Service Kurulum Rehberi (Server)

## Gereksinimler

- Docker Desktop veya Docker Engine
- Port 5060 bos olmali

---

## Adim 1: Docker Kurulumu (Windows Server)

### PowerShell ile (Admin olarak calistir):

```powershell
# Docker Desktop indirme sayfasi:
# https://docs.docker.com/desktop/setup/install/windows-install/

# VEYA winget ile:
winget install Docker.DockerDesktop

# Kurulum sonrasi restart gerekebilir
# Restart sonrasi Docker Desktop'i baslat
```

### Docker calistigini kontrol et:

```powershell
docker --version
docker ps
```

---

## Adim 2: PdfService Dosyalarini Olustur

Server'da bir klasor olustur (ornegin `C:\PdfService\`) ve icine 4 dosya koy:

### Dosya 1: `app.py`

```python
"""
PDF Generation Service
Receives HTML content and returns PDF
"""
from fastapi import FastAPI, HTTPException
from fastapi.responses import Response
from pydantic import BaseModel
from weasyprint import HTML, CSS
from weasyprint.text.fonts import FontConfiguration
import base64
from typing import Optional

app = FastAPI(title="PDF Service", version="1.0.0")

class PdfRequest(BaseModel):
    html: str
    filename: Optional[str] = "document.pdf"
    base_url: Optional[str] = None
    css: Optional[str] = None

class PdfBase64Request(BaseModel):
    html: str
    css: Optional[str] = None

# Default CSS for reports
DEFAULT_CSS = """
@page {
    size: A4;
    margin: 1.5cm;
}
body {
    font-family: Arial, sans-serif;
    font-size: 11pt;
    line-height: 1.4;
}
h1 { font-size: 18pt; color: #333; margin-bottom: 10px; }
h2 { font-size: 14pt; color: #555; margin-bottom: 8px; }
h3 { font-size: 12pt; color: #666; margin-bottom: 6px; }
table {
    width: 100%;
    border-collapse: collapse;
    margin: 10px 0;
}
th, td {
    border: 1px solid #ddd;
    padding: 6px 8px;
    text-align: left;
}
th {
    background-color: #f5f5f5;
    font-weight: bold;
}
tr:nth-child(even) {
    background-color: #fafafa;
}
.text-center { text-align: center; }
.text-right { text-align: right; }
.text-success { color: #28a745; }
.text-warning { color: #ffc107; }
.text-danger { color: #dc3545; }
.badge {
    display: inline-block;
    padding: 2px 6px;
    border-radius: 3px;
    font-size: 10pt;
}
.bg-success { background-color: #28a745; color: white; }
.bg-warning { background-color: #ffc107; color: black; }
.bg-danger { background-color: #dc3545; color: white; }
.card {
    border: 1px solid #ddd;
    border-radius: 4px;
    padding: 15px;
    margin: 10px 0;
}
.card-header {
    font-weight: bold;
    margin-bottom: 10px;
    padding-bottom: 5px;
    border-bottom: 1px solid #eee;
}
.summary-box {
    display: inline-block;
    text-align: center;
    padding: 10px 20px;
    margin: 5px;
    border: 1px solid #ddd;
    border-radius: 4px;
}
.summary-value {
    font-size: 24pt;
    font-weight: bold;
}
.summary-label {
    font-size: 9pt;
    color: #666;
}
.page-break { page-break-before: always; }
"""

@app.get("/health")
def health_check():
    return {"status": "healthy"}

@app.post("/generate")
def generate_pdf(request: PdfRequest):
    """Generate PDF from HTML and return as file download"""
    try:
        font_config = FontConfiguration()
        css = CSS(string=request.css or DEFAULT_CSS, font_config=font_config)

        html = HTML(string=request.html, base_url=request.base_url)
        pdf_bytes = html.write_pdf(stylesheets=[css], font_config=font_config)

        return Response(
            content=pdf_bytes,
            media_type="application/pdf",
            headers={
                "Content-Disposition": f'attachment; filename="{request.filename}"'
            }
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/generate-base64")
def generate_pdf_base64(request: PdfBase64Request):
    """Generate PDF from HTML and return as base64 string"""
    try:
        font_config = FontConfiguration()
        css = CSS(string=request.css or DEFAULT_CSS, font_config=font_config)

        html = HTML(string=request.html)
        pdf_bytes = html.write_pdf(stylesheets=[css], font_config=font_config)

        return {
            "pdf": base64.b64encode(pdf_bytes).decode("utf-8"),
            "content_type": "application/pdf"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5060)
```

### Dosya 2: `requirements.txt`

```
fastapi==0.115.0
uvicorn==0.32.0
cssselect2==0.7.0
tinycss2==1.4.0
pydyf==0.10.0
weasyprint==61.2
jinja2==3.1.4
python-multipart==0.0.12
```

### Dosya 3: `Dockerfile`

```dockerfile
FROM python:3.12-slim

# Install WeasyPrint dependencies
RUN apt-get update && apt-get install -y \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libgdk-pixbuf-2.0-0 \
    libffi-dev \
    shared-mime-info \
    fonts-liberation \
    fonts-dejavu-core \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py .

EXPOSE 5060

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "5060"]
```

### Dosya 4: `docker-compose.yml`

```yaml
services:
  pdf-service:
    build: .
    container_name: pdf-service
    ports:
      - "5060:5060"
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5060/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

---

## Adim 3: Servisi Baslat

```powershell
cd C:\PdfService
docker-compose up -d --build
```

Ilk seferde image build edilir (1-2 dk surer). Sonraki baslatmalarda hizli olur.

---

## Adim 4: Kontrol Et

```powershell
# Container durumu
docker ps

# Health check
curl http://localhost:5060/health
# Beklenen: {"status":"healthy"}

# PDF uretim testi
curl -X POST http://localhost:5060/generate -H "Content-Type: application/json" -d "{\"html\":\"<h1>Test</h1><p>PDF calisiyor</p>\",\"filename\":\"test.pdf\"}" -o test.pdf

# test.pdf dosyasi olusmalil
dir test.pdf
```

---

## Yonetim Komutlari

```powershell
# Servisi durdur
docker-compose down

# Servisi yeniden baslat
docker-compose restart

# Loglari gor
docker logs pdf-service

# Canli log takibi
docker logs -f pdf-service

# Yeniden build et (kod degisikligi sonrasi)
docker-compose up -d --build
```

---

## appsettings.json Ayari

.NET uygulamasinin `appsettings.json` dosyasinda su ayar olmali:

```json
{
  "PdfService": {
    "Url": "http://localhost:5060"
  }
}
```

---

## Sorun Giderme

| Sorun | Cozum |
|-------|-------|
| `docker: command not found` | Docker Desktop kurulu ve calistigi kontrol et |
| Port 5060 kullaniliyor | `netstat -ano \| findstr 5060` ile kontrol et, gerekirse docker-compose.yml'de portu degistir |
| Container baslamiyor | `docker logs pdf-service` ile hata mesajini oku |
| Health check failing | Container icine gir: `docker exec -it pdf-service bash` ve `curl localhost:5060/health` dene |
| .NET PDF hatasi | `appsettings.json`'da PdfService URL'inin dogru oldugunu kontrol et |
