# Script para validar se as duplicatas foram removidas

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Validação de Remoção de Duplicatas" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$sqlFile = Join-Path $scriptsPath "validar_remocao_duplicatas.sql"
if (Test-Path $sqlFile) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Lendo arquivo SQL..." -ForegroundColor Gray
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Executando validação no banco..." -ForegroundColor Yellow
    Write-Host ""
    
    $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    Write-Host "RESULTADOS DA VALIDAÇÃO:" -ForegroundColor Green
    Write-Host "========================" -ForegroundColor Green
    $output | ForEach-Object {
        Write-Host $_ -ForegroundColor White
    }
    
    Remove-Item $tempFile
    Write-Host ""
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Validação concluída!" -ForegroundColor Green
} else {
    Write-Host "[ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan

