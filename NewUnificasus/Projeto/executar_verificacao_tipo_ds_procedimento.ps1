# Script para verificar tipo de DS_PROCEDIMENTO

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_tipo_ds_procedimento.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_tipo_ds_procedimento.txt"

Write-Host "Verificando tipo de DS_PROCEDIMENTO..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default
