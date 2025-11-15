@echo off
echo ========================================
echo UnificaSUS - Iniciando Aplicacao
echo ========================================
echo.

cd /d "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"

echo Compilando projeto...
call dotnet build --no-restore

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERRO: Falha na compilacao!
    pause
    exit /b 1
)

echo.
echo Executando aplicacao...
echo.

dotnet run --project src\UnificaSUS.WPF\UnificaSUS.WPF.csproj

