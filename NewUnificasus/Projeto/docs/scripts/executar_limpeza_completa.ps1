# Script para executar limpeza completa de duplicatas em CID10 e CBO
# Preserva linhas em MAIÚSCULAS (sistema antigo)

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [switch]$ApenasVerificar = $false
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza de Duplicatas - CID10 e CBO" -ForegroundColor Cyan
Write-Host "Preservando linhas em MAIÚSCULAS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iniciando processo..." -ForegroundColor Gray
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Banco de dados: $DatabasePath" -ForegroundColor Gray
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Usuário: $User" -ForegroundColor Gray
Write-Host ""

if ($ApenasVerificar) {
    Write-Host "MODO: Apenas Verificação (não remove dados)" -ForegroundColor Yellow
    Write-Host ""
    
    # Apenas verificar
    $sqlFile = Join-Path $scriptsPath "verificar_duplicatas_cid_cbo.sql"
    if (Test-Path $sqlFile) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Lendo arquivo SQL: $sqlFile" -ForegroundColor Gray
        $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
        $content = $content.TrimStart([char]0xFEFF)
        $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
        
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Executando verificações no banco..." -ForegroundColor Yellow
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Conectando ao banco: $DatabasePath" -ForegroundColor Gray
        
        $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Resultados:" -ForegroundColor Green
        $output | ForEach-Object {
            Write-Host $_ -ForegroundColor White
        }
        
        Remove-Item $tempFile
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Verificação concluída!" -ForegroundColor Green
    } else {
        Write-Host "[ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
    }
} else {
    Write-Host "MODO: EXECUÇÃO COMPLETA (REMOVE duplicatas)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Pressione qualquer tecla para continuar ou Ctrl+C para cancelar..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Write-Host ""
    
    # 1. Limpar duplicatas em CID10
    Write-Host "1. Limpando duplicatas em RL_PROCEDIMENTO_CID (CID10)..." -ForegroundColor Yellow
    Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Iniciando processo..." -ForegroundColor Gray
    $sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cid_executar.sql"
    if (Test-Path $sqlFile) {
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Lendo arquivo SQL: $sqlFile" -ForegroundColor Gray
        $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
        $content = $content.TrimStart([char]0xFEFF)
        $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Executando script SQL no banco..." -ForegroundColor Gray
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Isso pode levar alguns minutos devido ao grande volume de dados..." -ForegroundColor Yellow
        
        $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Processamento concluído. Resultados:" -ForegroundColor Gray
        $output | ForEach-Object {
            Write-Host "   $_" -ForegroundColor White
        }
        
        Remove-Item $tempFile
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Limpeza de CID10 concluída!" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "   [ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
    }
    
    # 2. Limpar duplicatas em CBO
    Write-Host "2. Limpando duplicatas em RL_PROCEDIMENTO_OCUPACAO (CBO)..." -ForegroundColor Yellow
    Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Iniciando processo..." -ForegroundColor Gray
    $sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cbo_executar.sql"
    if (Test-Path $sqlFile) {
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Lendo arquivo SQL: $sqlFile" -ForegroundColor Gray
        $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
        $content = $content.TrimStart([char]0xFEFF)
        $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Executando script SQL no banco..." -ForegroundColor Gray
        
        $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Processamento concluído. Resultados:" -ForegroundColor Gray
        $output | ForEach-Object {
            Write-Host "   $_" -ForegroundColor White
        }
        
        Remove-Item $tempFile
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Limpeza de CBO concluída!" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "   [ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processo concluído!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

