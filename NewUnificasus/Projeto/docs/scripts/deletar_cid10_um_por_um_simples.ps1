# Script para deletar CID10 duplicados UM POR VEZ
# Versão simplificada que funciona

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$MaxDeletar = 10
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deletar CID10 Duplicados - Um por Um" -ForegroundColor Cyan
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
    
    # Buscar UM registro duplicado (o que NÃO é o menor INDICE do grupo)
    $sqlBuscar = @"
SELECT FIRST 1
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO
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
    
    $tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSql, $sqlBuscar, [System.Text.Encoding]::ASCII)
    
    $output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
    Remove-Item $tempSql -ErrorAction SilentlyContinue
    
    # Parsear resultado
    $indice = $null
    $coCid = $null
    $coProcedimento = $null
    
    foreach ($linha in $output) {
        if ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)') {
            $indice = $Matches[1]
            $coCid = $Matches[2]
            $coProcedimento = $Matches[3]
            break
        }
    }
    
    if (-not $indice) {
        Write-Host "[INFO] Nenhum registro duplicado encontrado!" -ForegroundColor Green
        break
    }
    
    # Mostrar registro
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Registro $($deletados + 1)/$MaxDeletar" -ForegroundColor Cyan
    Write-Host ">>> ENCONTRADO:" -ForegroundColor Yellow
    Write-Host "  INDICE: $indice" -ForegroundColor White
    Write-Host "  CO_CID: $coCid" -ForegroundColor White
    Write-Host "  CO_PROCEDIMENTO: $coProcedimento" -ForegroundColor White
    Write-Host "  DT_COMPETENCIA: $Competencia" -ForegroundColor White
    Write-Host ""
    
    # Deletar
    $sqlDelete = @"
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $indice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$coCid'
  AND CO_PROCEDIMENTO = '$coProcedimento';
"@
    
    $tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSql, $sqlDelete, [System.Text.Encoding]::ASCII)
    
    Write-Host "  [$(Get-Date -Format 'HH:mm:ss')] Deletando..." -ForegroundColor Yellow
    
    $deleteOutput = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" 2>&1
    $exitCode = $LASTEXITCODE
    Remove-Item $tempSql -ErrorAction SilentlyContinue
    
    if ($exitCode -eq 0) {
        # Verificar se realmente deletou
        $sqlVerificar = "SELECT COUNT(*) FROM RL_PROCEDIMENTO_CID WHERE INDICE = $indice AND DT_COMPETENCIA = '$Competencia';"
        $tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
        [System.IO.File]::WriteAllText($tempSql, $sqlVerificar, [System.Text.Encoding]::ASCII)
        $verificarOutput = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
        Remove-Item $tempSql -ErrorAction SilentlyContinue
        
        if ($verificarOutput -match '^\s*0\s*$') {
            Write-Host "  [OK] DELETADO com sucesso! (Total: $($deletados + 1))" -ForegroundColor Green
            $deletados++
        } else {
            Write-Host "  [AVISO] DELETE executou mas registro ainda existe" -ForegroundColor Yellow
            $deleteOutput | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
        }
    } else {
        Write-Host "  [ERRO] Falha ao deletar!" -ForegroundColor Red
        $deleteOutput | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
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

