@echo off
echo TI ZILL DIGITAL - Setup
echo =======================
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
  echo ERROR: .NET SDK not found. Download from https://dot.net
  pause
  exit /b 1
)
echo Restoring packages...
dotnet restore
echo.
echo Done! Run the app with:
echo   dotnet run
echo.
echo Or build a standalone .exe with:
echo   build.bat
pause
