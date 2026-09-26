@echo off
setlocal

set "WORKDIR=C:\Tools\KPlugNullGuardsFix"
set "PS1=%WORKDIR%\build_KPlugNullGuardsFix_v0.1.0.ps1"

echo.
echo ==========================================
echo   KPlugNullGuardsFix v0.1.0 - BUILD ONLY
echo ==========================================
echo.
echo This BAT does NOT install anything into the game.
echo.

if not exist "%PS1%" (
    echo ERROR: PowerShell script not found:
    echo %PS1%
    echo.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS1%"

set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
    echo FAILED. Exit code: %RC%
) else (
    echo SUCCESS.
    echo Output:
    echo %WORKDIR%\build\Test\KPlugNullGuardsFix.dll
)

echo.
pause
exit /b %RC%
