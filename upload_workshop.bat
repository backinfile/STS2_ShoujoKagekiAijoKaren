@echo off
setlocal EnableExtensions
chcp 65001 >nul

set MOD_NAME=ShoujoKagekiAijoKaren
set PACKAGE_ROOT=%~dp0..\%MOD_NAME%_dist
set MOD_CONTENT_DIR=%PACKAGE_ROOT%\%MOD_NAME%
set WORKSHOP_VDF=%~dp0workshop_item.vdf
set PREVIEW_FILE=%~dp0workshop_preview.jpg

echo ====================================
echo Slay the Spire 2 Workshop Upload
echo ====================================
echo.

set STEAM_USERNAME=backinfile
if not "%~1"=="" set STEAM_USERNAME=%~1

echo [1/5] Building release package...
call "%~dp0build.bat"
if errorlevel 1 (
    echo [ERROR] Build failed; upload cancelled.
    exit /b 1
)

echo.
echo [2/5] Checking Workshop content folder...
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

echo [3/5] Checking Workshop metadata...
if not exist "%WORKSHOP_VDF%" (
    echo [ERROR] Missing VDF: %WORKSHOP_VDF%
    exit /b 1
)
if not exist "%PREVIEW_FILE%" (
    echo [ERROR] Missing preview image: %PREVIEW_FILE%
    exit /b 1
)

echo [4/5] Locating SteamCMD...
if not "%STEAMCMD_PATH%"=="" (
    set STEAMCMD_EXE=%STEAMCMD_PATH%
) else if exist "D:\Tools\steamcmd\steamcmd.exe" (
    set STEAMCMD_EXE=D:\Tools\steamcmd\steamcmd.exe
) else (
    set STEAMCMD_EXE=steamcmd
)

where "%STEAMCMD_EXE%" >nul 2>nul
if errorlevel 1 (
    if not exist "%STEAMCMD_EXE%" (
        echo [ERROR] steamcmd.exe not found.
        echo Install SteamCMD, add it to PATH, or set STEAMCMD_PATH.
        echo Example:
        echo   set STEAMCMD_PATH=D:\Tools\steamcmd\steamcmd.exe
        exit /b 1
    )
)

echo [5/5] Uploading to Steam Workshop...
echo VDF: %WORKSHOP_VDF%
echo Content: %MOD_CONTENT_DIR%
echo.
echo If Steam Guard is required, SteamCMD will ask for it below.
echo.

"%STEAMCMD_EXE%" +login "%STEAM_USERNAME%" +workshop_build_item "%WORKSHOP_VDF%" +quit
if errorlevel 1 (
    echo [ERROR] SteamCMD upload failed.
    exit /b 1
)

echo.
echo ====================================
echo Workshop Upload Complete
echo ====================================
echo If this was the first upload, copy the publishedfileid from SteamCMD output
echo into workshop_item.vdf before future updates.
echo Keep visibility hidden until you verify the Workshop item page.
echo.
exit /b 0
