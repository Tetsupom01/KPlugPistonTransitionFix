@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo   KPlugNullGuardsFix v0.1.0 - BUILD ONLY
echo ==========================================
echo.
echo This BAT does NOT install anything into the game.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_KPlugNullGuardsFix_v0.1.0.ps1"

set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
    echo FAILED. Exit code: %RC%
) else (
    echo SUCCESS.
    echo Output:
    echo %~dp0build\Test\KPlugNullGuardsFix.dll
)

echo.
pause
exit /b %RC%
