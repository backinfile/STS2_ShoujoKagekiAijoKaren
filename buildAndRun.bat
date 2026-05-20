@echo off
chcp 65001 >nul

call build.bat
if errorlevel 1 (
    echo.
    echo [ERROR] Build failed, game will not launch.
    pause
    exit /b 1
)

echo.
echo [OK] Build successful, launching game...
start "" "D:\App\Stream\steamapps\common\Slay the Spire 2\launch_vulkan.bat"
