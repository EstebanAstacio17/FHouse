@echo off
title F House (FH) - Servidor Web
echo ========================================================
echo   🏠 F House (FH) - Plataforma Financiera Familiar
echo ========================================================
echo.
echo Compilando la solucion...
dotnet build "%~dp0FHouse.sln" -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Error al compilar la solucion.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Iniciando IIS Express en http://localhost:52419 ...
start "" "http://localhost:52419"
"C:\Program Files\IIS Express\iisexpress.exe" /path:"%~dp0FHouse.Web" /port:52419
pause
