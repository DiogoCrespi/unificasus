@echo off
REM Script para executar no servidor - Limpa duplicatas em lotes de 10
REM Mostra logs detalhados de cada registro removido

setlocal enabledelayedexpansion

set ISQL_PATH=C:\Program Files\Firebird\Firebird_3_0\isql.exe
set DB_PATH=E:\claupers\unificasus\UNIFICASUS.GDB
set SQL_FILE=%~dp0limpar_duplicatas_com_logs_detalhados.sql
set MAX_ITERACOES=100

echo ========================================
echo Limpeza de Duplicatas - Servidor
echo Processa 10 registros por vez
echo Mostra logs detalhados
echo ========================================
echo.

if not exist "%SQL_FILE%" (
    echo [ERRO] Arquivo SQL nao encontrado: %SQL_FILE%
    echo [INFO] Tentando arquivo alternativo...
    set SQL_FILE=%~dp0limpar_duplicatas_servidor.sql
    if not exist "%SQL_FILE%" (
        echo [ERRO] Nenhum arquivo SQL encontrado!
        pause
        exit /b 1
    )
)

set ITERACAO=0

:LOOP
set /a ITERACAO+=1
echo.
echo ========================================
echo [%DATE% %TIME%] Iteracao !ITERACAO!
echo ========================================
echo.

REM Primeiro, listar registros que serao removidos
echo [INFO] Listando registros que serao removidos...
if exist "%~dp0listar_duplicatas_simples.sql" (
    "%ISQL_PATH%" -user SYSDBA -password masterkey "%DB_PATH%" -i "%~dp0listar_duplicatas_simples.sql" -o "%TEMP%\lista_!ITERACAO!.txt" -q 2>&1
    if exist "%TEMP%\lista_!ITERACAO!.txt" (
        type "%TEMP%\lista_!ITERACAO!.txt"
    )
    echo.
) else if exist "%~dp0listar_duplicatas_antes.sql" (
    "%ISQL_PATH%" -user SYSDBA -password masterkey "%DB_PATH%" -i "%~dp0listar_duplicatas_antes.sql" -o "%TEMP%\lista_!ITERACAO!.txt" -q 2>&1
    if exist "%TEMP%\lista_!ITERACAO!.txt" (
        type "%TEMP%\lista_!ITERACAO!.txt"
    )
    echo.
) else (
    echo [AVISO] Script de listagem nao encontrado, continuando...
    echo.
)

REM Agora executar a limpeza
echo [INFO] Executando limpeza (removendo 10 registros)...
"%ISQL_PATH%" -user SYSDBA -password masterkey "%DB_PATH%" -i "%SQL_FILE%" -o "%TEMP%\limpeza_output_!ITERACAO!.txt" 2>&1

if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha na iteracao !ITERACAO!
    type "%TEMP%\limpeza_output_!ITERACAO!.txt"
    pause
    exit /b 1
)

echo.
echo [INFO] Resultado da iteracao !ITERACAO!:
echo ----------------------------------------
type "%TEMP%\limpeza_output_!ITERACAO!.txt"
echo ----------------------------------------
echo.

REM Verificar se ainda há duplicatas
echo [INFO] Verificando duplicatas restantes...
if exist "%~dp0check_duplicatas.sql" (
    "%ISQL_PATH%" -user SYSDBA -password masterkey "%DB_PATH%" -i "%~dp0check_duplicatas.sql" 2>&1
) else (
    echo [AVISO] Script de verificacao nao encontrado, continuando...
)

echo [%TIME%] Iteracao !ITERACAO! concluida
echo.

if !ITERACAO! LSS %MAX_ITERACOES% goto LOOP

echo ========================================
echo Processo concluido!
echo Total de iteracoes: !ITERACAO!
echo ========================================
echo.
echo [INFO] Verificando resultado final...
if exist "%~dp0check_duplicatas.sql" (
    "%ISQL_PATH%" -user SYSDBA -password masterkey "%DB_PATH%" -i "%~dp0check_duplicatas.sql" 2>&1
)

pause

