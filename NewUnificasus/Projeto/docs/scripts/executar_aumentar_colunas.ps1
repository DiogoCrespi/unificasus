# Script para aumentar colunas com acentos no banco Firebird remoto

$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"
$User = "SYSDBA"
$Password = "masterkey"
$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
$SqlFile = "$PSScriptRoot\aumentar_colunas_acentos.sql"

Write-Host "== Aumentando colunas para acomodar acentos ==" -ForegroundColor Cyan
Write-Host "Banco.............: $DatabasePath" -ForegroundColor Yellow
Write-Host "Usuário...........: $User"
Write-Host "Arquivo SQL.......: $SqlFile"
Write-Host ""

if (-not (Test-Path $IsqlPath)) {
    throw "isql.exe não encontrado em: $IsqlPath"
}

if (-not (Test-Path $SqlFile)) {
    throw "Arquivo SQL não encontrado: $SqlFile"
}

Write-Host ">>> ATENÇÃO: Este script vai alterar o tamanho das colunas NO_RUBRICA e NO_CID para 200 bytes." -ForegroundColor Yellow
Write-Host ">>> Pressione Ctrl+C para abortar ou Enter para continuar..."
[void][System.Console]::ReadLine()

Write-Host "Executando SQL..." -ForegroundColor Cyan

# Executa o SQL usando isql
$arguments = @(
    "-user", $User,
    "-password", $Password,
    $DatabasePath,
    "-i", $SqlFile
)

& $IsqlPath @arguments

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Colunas aumentadas com sucesso!" -ForegroundColor Green
    Write-Host "- NO_RUBRICA em TB_RUBRICA: 200 bytes" -ForegroundColor Green
    Write-Host "- NO_CID em TB_CID: 200 bytes" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Erro ao executar SQL. Código de saída: $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

