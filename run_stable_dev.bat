@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\dev_env.ps1" -Branch stable -Action Launch
exit /b %ERRORLEVEL%
