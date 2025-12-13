@echo off
echo ===================================
echo Excel Service Baslatiliyor...
echo ===================================
echo.

REM Python kontrolu
python --version >nul 2>&1
if errorlevel 1 (
    echo HATA: Python bulunamadi!
    echo Python yukleyin ve PATH'e ekleyin.
    pause
    exit /b 1
)

REM Virtual environment kontrolu ve olusturma
if not exist "venv" (
    echo Virtual environment olusturuluyor...
    python -m venv venv
)

REM Virtual environment aktif et
call venv\Scripts\activate.bat

REM Paketleri yukle
echo Paketler kontrol ediliyor...
pip install -r requirements.txt -q

echo.
echo ===================================
echo Servis http://localhost:8002 adresinde calisiyor
echo API Dokumantasyonu: http://localhost:8002/docs
echo Durdurmak icin Ctrl+C
echo ===================================
echo.

REM Servisi baslat
python main.py
