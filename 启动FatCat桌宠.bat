@echo off
setlocal
cd /d "%~dp0"

set "EXE=bin\Debug\net9.0-windows\FatCatPet.exe"

if not exist "%EXE%" (
    echo [FatCatPet] Program not found, building...
    dotnet build "FatCatPet.csproj" -v minimal
    if errorlevel 1 (
        echo [FatCatPet] Build failed.
        pause
        exit /b 1
    )
)

echo [FatCatPet] Starting desktop pet...
start "" "%EXE%"
endlocal
