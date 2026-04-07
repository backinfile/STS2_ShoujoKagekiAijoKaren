@echo off
chcp 65001 >nul
echo 正在启动 少女☆歌剧 Revue Starlight ...
godot --path .
if errorlevel 1 (
    echo.
    echo 启动失败，请确保 Godot 已添加到系统 PATH
    pause
)
