@echo off
echo Building TI ZILL DIGITAL...
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o publish\
echo.
echo Build complete! Output: publish\TiZillDigital.exe
pause
