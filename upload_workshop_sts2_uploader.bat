@echo off
setlocal EnableExtensions
chcp 65001 >nul

set MOD_NAME=ShoujoKagekiAijoKaren
set PUBLISHED_FILE_ID=3747532000
set PACKAGE_ROOT=%~dp0..\%MOD_NAME%_dist
set MOD_CONTENT_DIR=%PACKAGE_ROOT%\%MOD_NAME%
set PREVIEW_FILE=%~dp0workshop_preview.jpg
set WORKSHOP_CONFIG=%~dp0workshop.json
set UPLOADER_ROOT=%LOCALAPPDATA%\STS2ModUploader\sts2-mod-uploader
set WORKSPACE_ROOT=%LOCALAPPDATA%\STS2ModUploader\workspaces\%MOD_NAME%

if /I "%~1"=="--prepare-only" (
    set PREPARE_ONLY=1
) else (
    set PREPARE_ONLY=0
)

echo ====================================
echo Slay the Spire 2 Official Workshop Upload
echo ====================================
echo.

echo [1/6] Checking packaged mod files...
if not exist "%MOD_CONTENT_DIR%\%MOD_NAME%.dll" (
    echo [ERROR] Missing DLL: %MOD_CONTENT_DIR%\%MOD_NAME%.dll
    echo Run build.bat once before uploading with the STS2 uploader.
    exit /b 1
)
if not exist "%MOD_CONTENT_DIR%\%MOD_NAME%.pck" (
    echo [ERROR] Missing PCK: %MOD_CONTENT_DIR%\%MOD_NAME%.pck
    echo Run build.bat once before uploading with the STS2 uploader.
    exit /b 1
)
if not exist "%WORKSHOP_CONFIG%" (
    echo [ERROR] Missing STS2 uploader config: %WORKSHOP_CONFIG%
    exit /b 1
)
if not exist "%PREVIEW_FILE%" (
    echo [ERROR] Missing preview image: %PREVIEW_FILE%
    exit /b 1
)

echo [2/6] Updating packaged JSON...
copy /Y "%~dp0%MOD_NAME%.json" "%MOD_CONTENT_DIR%\%MOD_NAME%.json" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy %MOD_NAME%.json
    exit /b 1
)

echo [3/6] Fetching official STS2 mod uploader...
if not exist "%UPLOADER_ROOT%\.git" (
    mkdir "%LOCALAPPDATA%\STS2ModUploader" >nul 2>nul
    git clone --depth 1 https://github.com/megacrit/sts2-mod-uploader.git "%UPLOADER_ROOT%"
    if errorlevel 1 (
        echo [ERROR] Failed to clone sts2-mod-uploader.
        exit /b 1
    )
) else (
    git -C "%UPLOADER_ROOT%" pull --ff-only
    if errorlevel 1 (
        echo [ERROR] Failed to update sts2-mod-uploader.
        exit /b 1
    )
)

echo [4/6] Building official uploader...
dotnet build "%UPLOADER_ROOT%\ModUploader.csproj" -c Release
if errorlevel 1 (
    echo [ERROR] Failed to build sts2-mod-uploader.
    exit /b 1
)

set UPLOADER_EXE=%UPLOADER_ROOT%\bin\Release\net8.0\ModUploader.exe
if not exist "%UPLOADER_EXE%" (
    echo [ERROR] Built uploader executable not found: %UPLOADER_EXE%
    exit /b 1
)

echo [5/6] Preparing uploader workspace...
if exist "%WORKSPACE_ROOT%\content" (
    rmdir /S /Q "%WORKSPACE_ROOT%\content"
    if errorlevel 1 (
        echo [ERROR] Failed to clean workspace content: %WORKSPACE_ROOT%\content
        exit /b 1
    )
)
mkdir "%WORKSPACE_ROOT%\content" >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Failed to create workspace content: %WORKSPACE_ROOT%\content
    exit /b 1
)

copy /Y "%WORKSHOP_CONFIG%" "%WORKSPACE_ROOT%\workshop.json" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy workshop.json
    exit /b 1
)

> "%WORKSPACE_ROOT%\mod_id.txt" echo %PUBLISHED_FILE_ID%
if errorlevel 1 (
    echo [ERROR] Failed to write mod_id.txt
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.Drawing; $img=[System.Drawing.Image]::FromFile('%PREVIEW_FILE%'); try { $img.Save('%WORKSPACE_ROOT%\image.png', [System.Drawing.Imaging.ImageFormat]::Png) } finally { $img.Dispose() }"
if errorlevel 1 (
    echo [ERROR] Failed to prepare image.png
    exit /b 1
)

robocopy "%MOD_CONTENT_DIR%" "%WORKSPACE_ROOT%\content" /E >nul
if errorlevel 8 (
    echo [ERROR] Failed to copy packaged mod content into uploader workspace.
    exit /b 1
)

if "%PREPARE_ONLY%"=="1" (
    echo.
    echo Prepared STS2 uploader workspace:
    echo %WORKSPACE_ROOT%
    echo.
    echo No upload was performed because --prepare-only was supplied.
    exit /b 0
)

echo [6/6] Uploading through official STS2 uploader...
echo Workspace: %WORKSPACE_ROOT%
echo Item ID: %PUBLISHED_FILE_ID%
echo.

pushd "%UPLOADER_ROOT%\bin\Release\net8.0"
"%UPLOADER_EXE%" upload -w "%WORKSPACE_ROOT%" -i %PUBLISHED_FILE_ID%
set UPLOAD_ERROR=%ERRORLEVEL%
popd

if not "%UPLOAD_ERROR%"=="0" (
    echo [ERROR] STS2 uploader failed.
    exit /b %UPLOAD_ERROR%
)

echo.
echo ====================================
echo Official STS2 Workshop Upload Complete
echo ====================================
echo Tags are configured in workshop.json and submitted with SteamUGC.SetItemTags.
echo.
exit /b 0
