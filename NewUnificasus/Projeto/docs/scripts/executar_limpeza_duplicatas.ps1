# Script para executar limpeza de duplicatas em CID10 e CBO
# Preserva linhas em MAIÚSCULAS (sistema antigo)

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza de Duplicatas - CID10 e CBO" -ForegroundColor Cyan
Write-Host "Preservando linhas em MAIÚSCULAS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar duplicatas em CID10 antes da limpeza
Write-Host "1. Verificando duplicatas em RL_PROCEDIMENTO_CID (CID10)..." -ForegroundColor Yellow
$sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cid_preservar_maiusculas.sql"
if (Test-Path $sqlFile) {
    # Ler apenas as queries de verificação (não executar DELETE ainda)
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    
    # Extrair apenas as queries SELECT (até o comentário do DELETE)
    $selectQueries = $content -split "`n" | Where-Object { 
        $_ -match "^\s*SELECT" -or 
        $_ -match "^\s*--\s*[0-9]+\." -or
        $_ -match "^\s*$" -or
        $_ -match "^\s*--" 
    } | Select-Object -First 100
    
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $selectQueries -join "`n" | Out-File -FilePath $tempFile -Encoding ASCII
    
    Write-Host "   Executando verificações..." -ForegroundColor Gray
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 150
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "   Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

# 2. Verificar duplicatas em CBO antes da limpeza
Write-Host "2. Verificando duplicatas em RL_PROCEDIMENTO_OCUPACAO (CBO)..." -ForegroundColor Yellow
$sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cbo_preservar_maiusculas.sql"
if (Test-Path $sqlFile) {
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    
    $selectQueries = $content -split "`n" | Where-Object { 
        $_ -match "^\s*SELECT" -or 
        $_ -match "^\s*--\s*[0-9]+\." -or
        $_ -match "^\s*$" -or
        $_ -match "^\s*--" 
    } | Select-Object -First 100
    
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $selectQueries -join "`n" | Out-File -FilePath $tempFile -Encoding ASCII
    
    Write-Host "   Executando verificações..." -ForegroundColor Gray
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 150
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "   Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificação concluída!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ATENÇÃO: Para executar a limpeza, descomente os comandos DELETE" -ForegroundColor Yellow
Write-Host "nos arquivos SQL e execute novamente." -ForegroundColor Yellow

