@echo off
chcp 65001 >nul
echo ====================================
echo Slay the Spire 2 Mod Build Script
echo ====================================
echo.

set MOD_NAME=ShoujoKagekiAijoKaren
set PACKAGE_ROOT=%~dp0..\%MOD_NAME%_dist
set MOD_CONTENT_DIR=%PACKAGE_ROOT%\%MOD_NAME%
set GODOT_PATH=D:\Godot\megadot-4.5.1-m.12-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe

echo [1/5] Checking mod_manifest.json...
if not exist "mod_manifest.json" (
    echo [ERROR] mod_manifest.json not found
    exit /b 1
)

echo [2/5] Checking %MOD_NAME% folder...
if not exist "%MOD_NAME%" (
    echo [ERROR] %MOD_NAME% folder not found
    exit /b 1
)

echo [2.5/5] Preparing clean package folder...
if exist "%MOD_CONTENT_DIR%" (
    rmdir /S /Q "%MOD_CONTENT_DIR%"
    if errorlevel 1 (
        echo [ERROR] Failed to clean package folder: %MOD_CONTENT_DIR%
        exit /b 1
    )
)
mkdir "%MOD_CONTENT_DIR%"
if errorlevel 1 (
    echo [ERROR] Failed to create package folder: %MOD_CONTENT_DIR%
    exit /b 1
)

echo [3/5] Building C# code...
dotnet build --configuration ExportRelease
if errorlevel 1 (
    echo [ERROR] Build failed
    exit /b 1
)

echo [4/5] Exporting Godot .pck...
if exist "%GODOT_PATH%" (
    "%GODOT_PATH%" --headless --export-pack "Windows Desktop" "%MOD_CONTENT_DIR%\%MOD_NAME%.pck"
) else (
    echo [WARNING] Godot not found, skipping .pck export
)

echo [5/5] Copying release files...
copy /Y ".godot\mono\temp\bin\ExportRelease\%MOD_NAME%.dll" "%MOD_CONTENT_DIR%\"
if errorlevel 1 (
    echo [ERROR] Failed to copy DLL
    exit /b 1
)

copy /Y "%MOD_NAME%.json" "%MOD_CONTENT_DIR%\"
if errorlevel 1 (
    echo [WARNING] Failed to copy %MOD_NAME%.json
)

echo.
echo ====================================
echo Build Complete!
echo ====================================
echo DLL: %MOD_CONTENT_DIR%\%MOD_NAME%.dll
echo PCK: %MOD_CONTENT_DIR%\%MOD_NAME%.pck
echo JSON: %MOD_CONTENT_DIR%\%MOD_NAME%.json
echo.
exit /b 0
