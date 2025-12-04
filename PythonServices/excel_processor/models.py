from pydantic import BaseModel
from typing import List, Optional, Dict, Any
from enum import Enum

class ExcelColumnType(str, Enum):
    TEXT = "Text"
    NUMBER = "Number"
    DATE = "Date"
    BOOLEAN = "Boolean"
    EMAIL = "Email"
    PHONE = "Phone"
    DROPDOWN = "Dropdown"

class ExcelColumn(BaseModel):
    column_name: str
    property_name: str
    column_type: ExcelColumnType
    order: int
    is_required: bool = False
    validation_rules: Optional[Dict[str, Any]] = None
    dropdown_options: Optional[List[str]] = None
    sample_value: Optional[str] = None
    description: Optional[str] = None

class ExcelTemplate(BaseModel):
    name: str
    description: str
    entity_type: str
    sheet_name: str = "Sheet1"
    has_header: bool = True
    columns: List[ExcelColumn]

class ParsedRow(BaseModel):
    row_number: int
    data: Dict[str, Any]
    errors: List[str] = []
    is_valid: bool = True

class ExcelParseResult(BaseModel):
    total_rows: int
    valid_rows: int
    invalid_rows: int
    rows: List[ParsedRow]
