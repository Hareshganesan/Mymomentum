@echo off
echo Starting Momentum...
if exist "%~dp0src\Momentum\bin\Release\net8.0-windows10.0.19041.0\Momentum.exe" (
    start "" "%~dp0src\Momentum\bin\Release\net8.0-windows10.0.19041.0\Momentum.exe"
    exit
)
echo Building and running Momentum from source...
dotnet run --project "%~dp0src\Momentum\Momentum.csproj" -c Release
exit
