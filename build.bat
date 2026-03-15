@echo off
chcp 65001 >nul
echo ====================================
echo Slay the Spire 2 Mod Build Script
echo ====================================
echo.

set MOD_NAME=ShoujoKagekiAijoKaren
set GAME_MODS_DIR=D:\App\Stream\steamapps\common\Slay the Spire 2\mods
set GODOT_PATH=D:\Godot\megadot-4.5.1-m.8-windows-x86_64-llvm-editor-csharp\MegaDot_v4.5.1-stable_mono_win64.exe

echo [1/5] Checking mod_manifest.json...
if not exist "mod_manifest.json" (
    echo [ERROR] mod_manifest.json not found
    pause
    exit /b 1
)

echo [2/5] Checking %MOD_NAME% folder...
if not exist "%MOD_NAME%" (
    echo [ERROR] %MOD_NAME% folder not found
    pause
    exit /b 1
)

echo [3/5] Building C# code...
dotnet build --configuration ExportRelease
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo [4/5] Exporting Godot .pck...
if exist "%GODOT_PATH%" (
    "%GODOT_PATH%" --headless --export-pack "Windows Desktop" "%GAME_MODS_DIR%\%MOD_NAME%.pck"
) else (
    echo [WARNING] Godot not found, skipping .pck export
)

echo [5/5] Copying files to mods folder...
if not exist "%GAME_MODS_DIR%" (
    echo [ERROR] Mods folder not found: %GAME_MODS_DIR%
    pause
    exit /b 1
)

copy /Y ".godot\mono\temp\bin\ExportRelease\%MOD_NAME%.dll" "%GAME_MODS_DIR%\"
if errorlevel 1 (
    echo [ERROR] Failed to copy DLL
    pause
    exit /b 1
)

echo.
echo ====================================
echo Build Complete!
echo ====================================
echo DLL: %GAME_MODS_DIR%\%MOD_NAME%.dll
echo PCK: %GAME_MODS_DIR%\%MOD_NAME%.pck
echo.
pause
