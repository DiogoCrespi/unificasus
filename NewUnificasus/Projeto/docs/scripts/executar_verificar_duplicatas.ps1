# Script para verificar duplicatas em RL_PROCEDIMENTO_CID

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificando Duplicatas em RL_PROCEDIMENTO_CID" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$sqlFile = Join-Path $scriptsPath "verificar_duplicatas_cid.sql"
if (Test-Path $sqlFile) {
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "Executando verificações..." -ForegroundColor Yellow
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 200
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificação concluída!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

