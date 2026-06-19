@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "MOD_NAME=ShoujoKagekiAijoKaren"
set "DEFAULT_MODS_DIR=D:\App\Stream\steamapps\common\Slay the Spire 2\mods"

if not "%~1"=="" (
    set "MODS_DIR=%~1"
) else if not "%STS2_MODS_DIR%"=="" (
    set "MODS_DIR=%STS2_MODS_DIR%"
) else (
    set "MODS_DIR=%DEFAULT_MODS_DIR%"
)

set "INSTALL_DIR=%MODS_DIR%\%MOD_NAME%"

echo ====================================
echo Slay the Spire 2 Local Mod Remove
echo ====================================
echo Mods:   %MODS_DIR%
echo Target: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" (
    echo [OK] Mod is not installed at target folder.
    exit /b 0
)

if not exist "%INSTALL_DIR%\%MOD_NAME%.json" (
    echo [ERROR] Refusing to delete: expected marker file is missing.
    echo Marker: %INSTALL_DIR%\%MOD_NAME%.json
    exit /b 1
)

echo Removing local mod folder...
rmdir /S /Q "%INSTALL_DIR%"
if errorlevel 1 (
    echo [ERROR] Failed to remove: %INSTALL_DIR%
    exit /b 1
)

echo.
echo ====================================
echo Local Mod Remove Complete
echo ====================================
echo Removed:
echo   %INSTALL_DIR%
echo.
exit /b 0
