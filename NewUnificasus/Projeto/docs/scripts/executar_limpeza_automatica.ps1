# Script para executar limpeza automática de duplicatas
# Processa 1000 registros por vez até não haver mais duplicatas

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [int]$MaxIteracoes = 200
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Limpeza Automática de Duplicatas" -ForegroundColor Cyan
Write-Host "Processa 10 registros por vez" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iniciando processo..." -ForegroundColor Gray
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Banco: $DatabasePath" -ForegroundColor Gray
Write-Host ""

$sqlFile = Join-Path $scriptsPath "limpar_duplicatas_cid_automatico.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "[ERRO] Arquivo não encontrado: $sqlFile" -ForegroundColor Red
    exit 1
}

$iteracao = 0
$totalRemovido = 0

while ($iteracao -lt $MaxIteracoes) {
    $iteracao++
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iteração $iteracao..." -ForegroundColor Yellow
    
    # Ler e executar script
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Executando DELETE (até 10 registros)..." -ForegroundColor Gray
    
    try {
        $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        # Verificar quantas duplicatas restam
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
        
        # Verificar se houve erro no DELETE
        $temErro = $false
        foreach ($line in $output) {
            if ($line -match 'error|Error|ERROR|failed|Failed|FAILED') {
                Write-Host "   [ERRO] $line" -ForegroundColor Red
                $temErro = $true
            }
        }
        
        if (-not $temErro) {
            Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] DELETE executado com sucesso" -ForegroundColor Green
        }
        
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Grupos duplicados restantes: $duplicatasRestantes" -ForegroundColor $(if ($duplicatasRestantes -eq 0) { "Green" } else { "Yellow" })
        
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        Remove-Item $checkFile -ErrorAction SilentlyContinue
        
        # Validar se realmente removeu registros
        $totalSql = @"
SELECT COUNT(*) AS TOTAL
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';
"@
        $totalFile = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($totalFile, $totalSql, [System.Text.Encoding]::ASCII)
        $totalOutput = & $IsqlPath -user $User -password $Password $DatabasePath -i $totalFile 2>&1
        
        $totalRegistros = 0
        foreach ($line in $totalOutput) {
            if ($line -match '^\s*(\d+)\s*$') {
                $totalRegistros = [int]$Matches[1]
                break
            }
        }
        Write-Host "   [$(Get-Date -Format 'HH:mm:ss')] Total de registros: $totalRegistros" -ForegroundColor Cyan
        Remove-Item $totalFile -ErrorAction SilentlyContinue
        
        if ($duplicatasRestantes -eq 0) {
            Write-Host ""
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Todas as duplicatas foram removidas!" -ForegroundColor Green
            break
        }
        
        # Pequena pausa entre iterações
        Start-Sleep -Milliseconds 100
    }
    catch {
        Write-Host "   [ERRO] Erro na iteração $iteracao : $_" -ForegroundColor Red
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        break
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processo concluído!" -ForegroundColor Green
Write-Host "Iterações executadas: $iteracao" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

