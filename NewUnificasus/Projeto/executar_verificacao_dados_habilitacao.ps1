# Script para verificar dados de TB_HABILITACAO

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_dados_habilitacao.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_dados_habilitacao.txt"

Write-Host "Verificando dados de TB_HABILITACAO..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default
