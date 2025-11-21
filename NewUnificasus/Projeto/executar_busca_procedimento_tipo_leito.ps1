# Script para buscar procedimentos com tipo de leito no Firebird

$isqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$dbPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$sqlFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\buscar_procedimento_com_tipo_leito.sql"
$outputFile = "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\resultado_procedimento_tipo_leito.txt"

Write-Host "Executando consulta no Firebird..." -ForegroundColor Cyan

# Executar o SQL e salvar resultado
& $isqlPath -user SYSDBA -password masterkey $dbPath -i $sqlFile -o $outputFile

Write-Host "Resultado salvo em: $outputFile" -ForegroundColor Green

# Exibir o resultado
Write-Host "`nResultado da consulta:" -ForegroundColor Yellow
Get-Content $outputFile -Encoding Default
