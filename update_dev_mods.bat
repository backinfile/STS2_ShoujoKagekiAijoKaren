@echo off
setlocal
call "%~dp0build.bat"
if errorlevel 1 exit /b %ERRORLEVEL%
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\dev_env.ps1" -Branch stable -Action Install
if errorlevel 1 exit /b %ERRORLEVEL%
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\dev_env.ps1" -Branch beta -Action Install
exit /b %ERRORLEVEL%
