@echo off
setlocal
chcp 65001 >nul
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build_dual_branch.ps1" %*
exit /b %ERRORLEVEL%
