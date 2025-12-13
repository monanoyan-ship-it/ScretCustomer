@echo off
echo ===================================
echo Excel Service Testleri
echo ===================================
echo.

REM Virtual environment aktif et
if exist "venv\Scripts\activate.bat" (
    call venv\Scripts\activate.bat
)

REM requests paketi kontrolu
pip show requests >nul 2>&1
if errorlevel 1 (
    echo requests paketi yukleniyor...
    pip install requests -q
)

echo Testler baslatiliyor...
echo Oncelikle servisin calistigindan emin olun!
echo.

python test_service.py

echo.
pause
