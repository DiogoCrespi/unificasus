# Script SIMPLES para deletar CID10 duplicados UM POR VEZ
# Versão simplificada que funciona melhor com isql

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$MaxDeletar = 10
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deletar CID10 Duplicados" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host "Maximo: $MaxDeletar registros" -ForegroundColor Gray
Write-Host ""

$deletados = 0
$iteracao = 0

while ($deletados -lt $MaxDeletar) {
    $iteracao++
    
    # Buscar UM registro duplicado
    $sqlBuscar = @"
SELECT FIRST 1
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    SUBSTRING(pc.NO_CID FROM 1 FOR 60) AS NO_CID
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '$Competencia'
  AND EXISTS (
      SELECT 1
      FROM RL_PROCEDIMENTO_CID pc2
      WHERE pc2.DT_COMPETENCIA = '$Competencia'
        AND pc2.CO_CID = pc.CO_CID
        AND pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
      GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
      HAVING COUNT(*) > 1
  )
  AND pc.INDICE <> (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID pc3
      WHERE pc3.DT_COMPETENCIA = '$Competencia'
        AND pc3.CO_CID = pc.CO_CID
        AND pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
  )
ORDER BY pc.INDICE;
"@
    
    $tempSqlBuscar = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSqlBuscar, $sqlBuscar, [System.Text.Encoding]::ASCII)
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iteracao $iteracao - Buscando proximo registro..." -ForegroundColor Yellow
    
    $output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSqlBuscar" -q 2>&1
    Remove-Item $tempSqlBuscar -ErrorAction SilentlyContinue
    
    # Parsear resultado (formato: INDICE CO_CID CO_PROCEDIMENTO NO_CID)
    $indice = $null
    $coCid = $null
    $coProcedimento = $null
    $noCid = $null
    
    foreach ($linha in $output) {
        if ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)\s+(.+)$') {
            $indice = $Matches[1]
            $coCid = $Matches[2]
            $coProcedimento = $Matches[3]
            $noCid = $Matches[4].Trim()
            break
        }
    }
    
    if (-not $indice) {
        Write-Host "[INFO] Nenhum registro duplicado encontrado!" -ForegroundColor Green
        break
    }
    
    # Mostrar registro
    Write-Host ""
    Write-Host ">>> REGISTRO ENCONTRADO:" -ForegroundColor Cyan
    Write-Host "  INDICE: $indice" -ForegroundColor White
    Write-Host "  CO_CID: $coCid" -ForegroundColor White
    Write-Host "  CO_PROCEDIMENTO: $coProcedimento" -ForegroundColor White
    Write-Host "  NO_CID: $noCid" -ForegroundColor White
    Write-Host ""
    
    # Deletar
    $sqlDeletar = @"
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $indice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$coCid'
  AND CO_PROCEDIMENTO = '$coProcedimento';
"@
    
    $tempSqlDeletar = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSqlDeletar, $sqlDeletar, [System.Text.Encoding]::ASCII)
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Deletando..." -ForegroundColor Yellow
    
    $deleteOutput = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSqlDeletar" -q 2>&1
    Remove-Item $tempSqlDeletar -ErrorAction SilentlyContinue
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] DELETADO! (Total: $($deletados + 1))" -ForegroundColor Green
        $deletados++
    } else {
        Write-Host "[ERRO] Falha ao deletar!" -ForegroundColor Red
        $deleteOutput | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        break
    }
    
    Write-Host ""
    Write-Host "----------------------------------------" -ForegroundColor Gray
    Write-Host ""
    
    Start-Sleep -Milliseconds 200
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Concluido!" -ForegroundColor Green
Write-Host "  Total deletados: $deletados" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

