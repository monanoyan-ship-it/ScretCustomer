from fastapi import FastAPI, File, UploadFile, HTTPException, Form
from fastapi.responses import StreamingResponse
from fastapi.middleware.cors import CORSMiddleware
from models import ExcelTemplate, ExcelParseResult
from excel_handler import ExcelHandler
import logging
import io
import json

app = FastAPI(
    title="Excel Processor Service",
    description="Excel import/export islemleri icin mikro servis",
    version="1.0.0"
)

# CORS ayarlari - .NET API'den erisilebilmesi icin
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

excel_handler = ExcelHandler()

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@app.get("/")
async def root():
    """Ana sayfa - API bilgileri"""
    return {
        "service": "Excel Processor Service",
        "version": "1.0.0",
        "endpoints": {
            "health": "/health",
            "generate_template": "POST /generate-template",
            "parse_excel": "POST /parse-excel",
            "docs": "/docs"
        }
    }


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "service": "excel-processor"}


@app.post("/generate-template")
async def generate_template(template: ExcelTemplate):
    """
    Excel template olusturur ve indirir

    Ornek request body:
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
    """
    try:
        logger.info(f"Generating template: {template.name}")

        excel_bytes = excel_handler.generate_template_excel(template)

        filename = f"{template.name.replace(' ', '_')}_template.xlsx"

        return StreamingResponse(
            io.BytesIO(excel_bytes),
            media_type="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            headers={"Content-Disposition": f"attachment; filename={filename}"}
        )

    except Exception as e:
        logger.error(f"Template generation failed: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/parse-excel", response_model=ExcelParseResult)
async def parse_excel(
    file: UploadFile = File(...),
    template: str = Form(...)
):
    """
    Excel dosyasini parse eder ve validate eder

    - file: Excel dosyasi (.xlsx)
    - template: JSON formatinda template bilgisi
    """
    try:
        logger.info(f"Parsing Excel file: {file.filename}")

        # Template JSON'u parse et
        template_dict = json.loads(template)
        template_obj = ExcelTemplate(**template_dict)

        # Excel dosyasini oku
        file_content = await file.read()

        # Parse ve validate
        result = excel_handler.parse_excel(file_content, template_obj)

        logger.info(f"Parsing completed: {result.valid_rows}/{result.total_rows} valid rows")

        return result

    except json.JSONDecodeError as e:
        logger.error(f"Invalid template JSON: {e}")
        raise HTTPException(status_code=400, detail="Template JSON formati gecersiz")
    except Exception as e:
        logger.error(f"Excel parsing failed: {e}")
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8002)
