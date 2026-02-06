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
