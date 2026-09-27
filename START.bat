@echo off
title PD400X HID -> Discord
cd /d "%~dp0"
taskkill /f /im HidWatch.exe >nul 2>nul
timeout /t 1 /nobreak >nul
set CSC=
if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if "%CSC%"=="" if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if "%CSC%"=="" (
  echo csc.exe not found
  pause
  exit /b 1
)
echo Building HidWatch.exe ...
"%CSC%" /nologo /platform:x64 /t:exe /out:"%~dp0HidWatch.exe" "%~dp0HidWatch.cs"
if errorlevel 1 (
  echo Build failed. Close old HidWatch window and run START.bat again.
  pause
  exit /b 1
)
echo.
echo Close Maono Link if it is running.
echo Close Discord Keybinds page.
echo Then tap mute on the microphone.
echo.
"%~dp0HidWatch.exe"
echo.
pause
