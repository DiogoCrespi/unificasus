# Script para verificar competência em detalhes

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_competencia_detalhes.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_competencia_detalhes.txt"

Write-Host "Verificando competência em detalhes..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default
