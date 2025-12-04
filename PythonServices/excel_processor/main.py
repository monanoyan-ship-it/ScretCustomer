from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import StreamingResponse
from models import ExcelTemplate, ExcelParseResult
from excel_handler import ExcelHandler
import logging
import io

app = FastAPI(title="Excel Processor Service")
excel_handler = ExcelHandler()

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "service": "excel-processor"}

@app.post("/generate-template")
async def generate_template(template: ExcelTemplate):
    """
    Excel template oluşturur ve indirir
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
    template: str = File(...)  # JSON string olarak gelecek
):
    """
    Excel dosyasını parse eder ve validate eder
    """
    try:
        logger.info(f"Parsing Excel file: {file.filename}")

        # Template JSON'u parse et
        import json
        template_dict = json.loads(template)
        template_obj = ExcelTemplate(**template_dict)

        # Excel dosyasını oku
        file_content = await file.read()

        # Parse ve validate
        result = excel_handler.parse_excel(file_content, template_obj)

        logger.info(f"Parsing completed: {result.valid_rows}/{result.total_rows} valid rows")

        return result

    except Exception as e:
        logger.error(f"Excel parsing failed: {e}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8002)
