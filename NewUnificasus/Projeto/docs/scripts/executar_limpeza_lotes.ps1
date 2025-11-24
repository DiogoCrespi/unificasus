# Script para executar limpeza de duplicatas em lotes pequenos
# Processa um grupo de duplicatas por vez para melhor performance

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [int]$MaxLotes = 1000
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza de Duplicatas em Lotes" -ForegroundColor Cyan
Write-Host "Preservando linhas em MAIÚSCULAS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iniciando processo..." -ForegroundColor Gray
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Banco: $DatabasePath" -ForegroundColor Gray
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Máximo de lotes: $MaxLotes" -ForegroundColor Gray
Write-Host ""

$sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cid_lote_unico.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "[ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
    exit 1
}

$loteAtual = 0
$totalRemovido = 0

while ($loteAtual -lt $MaxLotes) {
    $loteAtual++
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processando lote $loteAtual..." -ForegroundColor Yellow
    
    # Ler e executar script
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Executando DELETE..." -ForegroundColor Gray
    
    try {
        $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        # Verificar se ainda há duplicatas
        $checkSql = @"
SELECT 
    COUNT(*) AS GRUPOS_DUPLICATAS
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);
"@
        
        $checkFile = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($checkFile, $checkSql, [System.Text.Encoding]::ASCII)
        
        $checkOutput = & $IsqlPath -user $User -password $Password $DatabasePath -i $checkFile 2>&1
        
        # Extrair número de duplicatas restantes
        $duplicatasRestantes = 0
        foreach ($line in $checkOutput) {
            if ($line -match '^\s*(\d+)\s*$') {
                $duplicatasRestantes = [int]$Matches[1]
                break
            }
        }
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Duplicatas restantes: $duplicatasRestantes" -ForegroundColor $(if ($duplicatasRestantes -eq 0) { "Green" } else { "Yellow" })
        
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        Remove-Item $checkFile -ErrorAction SilentlyContinue
        
        if ($duplicatasRestantes -eq 0) {
            Write-Host ""
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Todas as duplicatas foram removidas!" -ForegroundColor Green
            break
        }
        
        # Pequena pausa entre lotes para não sobrecarregar o banco
        Start-Sleep -Milliseconds 100
    }
    catch {
        Write-Host "   [ERRO] Erro ao processar lote $loteAtual : $_" -ForegroundColor Red
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        break
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processo concluído!" -ForegroundColor Green
Write-Host "Lotes processados: $loteAtual" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

