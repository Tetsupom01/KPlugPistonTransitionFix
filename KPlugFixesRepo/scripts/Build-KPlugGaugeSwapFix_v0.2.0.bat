@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo  KPlug Gauge Swap Fix v0.2.0
echo ============================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-KPlugGaugeSwapFix_v0.2.0.ps1"

echo.
if errorlevel 1 (
    echo BUILD FAILED.
) else (
    echo BUILD COMPLETED.
)
echo.
pause
