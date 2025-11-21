# Script para verificar estrutura de Instrumento de Registro

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_instrumento_registro.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_instrumento_registro.txt"

Write-Host "Verificando estrutura de Instrumento de Registro..." -ForegroundColor Cyan

& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "`nResultado salvo em: $outputFile" -ForegroundColor Green
Write-Host "`nPrimeiras linhas do resultado:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default | Select-Object -First 100
