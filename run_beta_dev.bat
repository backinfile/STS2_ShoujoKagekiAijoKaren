@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\dev_env.ps1" -Branch beta -Action Launch
exit /b %ERRORLEVEL%
