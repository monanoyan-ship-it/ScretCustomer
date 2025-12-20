# SecretCustomer IIS Publish Script
# Bu script projeyi IIS icin publish eder

param(
    [string]$OutputPath = "C:\inetpub\wwwroot\SecretCustomer",
    [string]$Configuration = "Release"
)

Write-Host "SecretCustomer IIS Publish Basladi..." -ForegroundColor Green
Write-Host "Cikis Klasoru: $OutputPath" -ForegroundColor Cyan

# Proje dizinine git
$projectPath = "$PSScriptRoot\Backend\SecretCustomer.API"
Set-Location $projectPath

# Onceki publish'i temizle
if (Test-Path $OutputPath) {
    Write-Host "Onceki publish temizleniyor..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Projeyi publish et
Write-Host "Proje publish ediliyor..." -ForegroundColor Cyan
dotnet publish -c $Configuration -o $OutputPath --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "PUBLISH BASARILI!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sonraki Adimlar:" -ForegroundColor Yellow
    Write-Host "1. appsettings.Production.json dosyasindaki sifreleri guncelleyin" -ForegroundColor White
    Write-Host "2. IIS'de yeni bir site olusturun: $OutputPath" -ForegroundColor White
    Write-Host "3. Application Pool'u 'No Managed Code' olarak ayarlayin" -ForegroundColor White
    Write-Host "4. Site'a HTTPS binding ekleyin" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "PUBLISH BASARISIZ!" -ForegroundColor Red
    exit 1
}
