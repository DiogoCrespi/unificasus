# Script para verificar tamanhos das colunas no banco

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\docs\scripts\verificar_tamanhos_colunas.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\docs\scripts\resultado_tamanhos_colunas.txt"

Write-Host "Verificando tamanhos das colunas no banco..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default

