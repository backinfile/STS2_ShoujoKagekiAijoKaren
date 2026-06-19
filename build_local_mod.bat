@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "MOD_NAME=ShoujoKagekiAijoKaren"
set "PROJECT_DIR=%~dp0"
set "MOD_CONTENT_DIR=%PROJECT_DIR%%MOD_NAME%"
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
echo Slay the Spire 2 Local Mod Install
echo ====================================
echo Project: %PROJECT_DIR%
echo Package: %MOD_CONTENT_DIR%
echo Mods:    %MODS_DIR%
echo Target:  %INSTALL_DIR%
echo.

echo [1/4] Building local package...
call "%PROJECT_DIR%build.bat"
if errorlevel 1 (
    echo [ERROR] Build failed; local install cancelled.
    exit /b 1
)

echo.
echo [2/4] Checking package files...
if not exist "%MOD_CONTENT_DIR%\%MOD_NAME%.dll" (
    echo [ERROR] Missing DLL: %MOD_CONTENT_DIR%\%MOD_NAME%.dll
    exit /b 1
)
if not exist "%MOD_CONTENT_DIR%\%MOD_NAME%.pck" (
    echo [ERROR] Missing PCK: %MOD_CONTENT_DIR%\%MOD_NAME%.pck
    exit /b 1
)
if not exist "%MOD_CONTENT_DIR%\%MOD_NAME%.json" (
    echo [ERROR] Missing JSON: %MOD_CONTENT_DIR%\%MOD_NAME%.json
    exit /b 1
)

echo [3/4] Preparing mods folder...
if not exist "%MODS_DIR%" (
    mkdir "%MODS_DIR%"
    if errorlevel 1 (
        echo [ERROR] Failed to create mods folder: %MODS_DIR%
        exit /b 1
    )
)
if not exist "%INSTALL_DIR%" (
    mkdir "%INSTALL_DIR%"
    if errorlevel 1 (
        echo [ERROR] Failed to create target folder: %INSTALL_DIR%
        exit /b 1
    )
)

echo [4/4] Copying package to local mods...
robocopy "%MOD_CONTENT_DIR%" "%INSTALL_DIR%" /E /R:2 /W:1 /NFL /NDL /NP
set "ROBOCOPY_EXIT=%ERRORLEVEL%"
if %ROBOCOPY_EXIT% GEQ 8 (
    echo [ERROR] Robocopy failed with exit code %ROBOCOPY_EXIT%.
    exit /b %ROBOCOPY_EXIT%
)

echo.
echo ====================================
echo Local Mod Install Complete
echo ====================================
echo Installed to:
echo   %INSTALL_DIR%
echo.
exit /b 0
