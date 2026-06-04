# Automation Script to Build and Compile eGPU Config Switcher Installer
$ErrorActionPreference = "Stop"

Write-Host "1. Killing any active eGPUConfigSwitcher process..." -ForegroundColor Cyan
try {
    Stop-Process -Name "eGPUConfigSwitcher" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
} catch { }

Write-Host "2. Cleaning up publish directories..." -ForegroundColor Cyan
$PublishDir = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\publish"
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}

Write-Host "3. Publishing eGPUConfigSwitcher project..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained false -o $PublishDir

Write-Host "4. Compiling Inno Setup Script..." -ForegroundColor Cyan
$IsccPath = "C:\Users\Brian\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $IsccPath)) {
    throw "ISCC.exe not found at '$IsccPath'. Please check Inno Setup installation."
}

& $IsccPath (Join-Path $PSScriptRoot "installer.iss")

Write-Host "5. Verifying Setup output..." -ForegroundColor Cyan
$SetupPath = Join-Path $PSScriptRoot "Output\eGPUConfigSwitcherSetup.exe"
if (Test-Path $SetupPath) {
    Write-Host "SUCCESS! Installer generated at: $SetupPath" -ForegroundColor Green
} else {
    throw "Installer generation failed."
}
