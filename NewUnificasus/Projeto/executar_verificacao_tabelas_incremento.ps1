# Script para verificar tabelas de incremento

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_tabelas_incremento.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_tabelas_incremento.txt"

Write-Host "Verificando tabelas de incremento..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default
