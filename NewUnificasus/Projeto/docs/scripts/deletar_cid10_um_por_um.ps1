# Script para deletar CID10 duplicados UM POR VEZ
# Mostra cada registro antes de deletar

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$MaxDeletar = 10,
    [switch]$ConfirmarCada = $false
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deletar CID10 Duplicados - Um por Um" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host "Maximo a deletar: $MaxDeletar registros" -ForegroundColor Gray
Write-Host ""

# Script SQL para buscar duplicados
$sqlBuscar = @"
SELECT 
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    SUBSTRING(pc.NO_CID FROM 1 FOR 70) AS NO_CID,
    CASE 
        WHEN UPPER(pc.NO_CID) = pc.NO_CID THEN 'MAIUSCULA'
        ELSE 'MISTO'
    END AS TIPO
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
ORDER BY pc.INDICE
ROWS 1 TO $MaxDeletar;
"@

$tempSqlBuscar = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlBuscar, $sqlBuscar, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Buscando duplicados..." -ForegroundColor Yellow

# Executar busca e capturar resultado
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSqlBuscar" -q 2>&1

# Parsear resultado (formato do isql)
$registros = @()
$linhas = $output | Where-Object { $_ -match '^\s*\d+\s+' }

foreach ($linha in $linhas) {
    if ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)\s+(\d{6})\s+(.{1,70})\s+(MAIUSCULA|MISTO)') {
        $registros += [PSCustomObject]@{
            INDICE = $Matches[1]
            CO_CID = $Matches[2]
            CO_PROCEDIMENTO = $Matches[3]
            DT_COMPETENCIA = $Matches[4]
            NO_CID = $Matches[5].Trim()
            TIPO = $Matches[6]
        }
    }
}

Remove-Item $tempSqlBuscar -ErrorAction SilentlyContinue

if ($registros.Count -eq 0) {
    Write-Host "[AVISO] Nenhum registro duplicado encontrado!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Output do isql:" -ForegroundColor Gray
    $output | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
    exit 0
}

Write-Host "[OK] Encontrados $($registros.Count) registros para deletar" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$deletados = 0
$erros = 0

foreach ($reg in $registros) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Registro $($deletados + 1)/$($registros.Count)" -ForegroundColor Cyan
    Write-Host "  INDICE: $($reg.INDICE)" -ForegroundColor White
    Write-Host "  CO_CID: $($reg.CO_CID)" -ForegroundColor White
    Write-Host "  CO_PROCEDIMENTO: $($reg.CO_PROCEDIMENTO)" -ForegroundColor White
    Write-Host "  NO_CID: $($reg.NO_CID)" -ForegroundColor White
    Write-Host "  TIPO: $($reg.TIPO)" -ForegroundColor $(if ($reg.TIPO -eq 'MAIUSCULA') { 'Yellow' } else { 'White' })
    Write-Host ""
    
    if ($ConfirmarCada) {
        $confirm = Read-Host "  Deletar este registro? (S/N)"
        if ($confirm -ne "S" -and $confirm -ne "s") {
            Write-Host "  [PULADO] Registro não deletado" -ForegroundColor Yellow
            Write-Host ""
            continue
        }
    }
    
    # Criar script SQL para deletar
    $sqlDeletar = @"
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $($reg.INDICE)
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$($reg.CO_CID)'
  AND CO_PROCEDIMENTO = '$($reg.CO_PROCEDIMENTO)';
"@
    
    $tempSqlDeletar = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSqlDeletar, $sqlDeletar, [System.Text.Encoding]::ASCII)
    
    # Executar delete
    $deleteOutput = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSqlDeletar" -q 2>&1
    
    Remove-Item $tempSqlDeletar -ErrorAction SilentlyContinue
    
    # Verificar se deletou (isql retorna vazio se sucesso, ou erro se falhou)
    if ($LASTEXITCODE -eq 0 -and ($deleteOutput -eq $null -or $deleteOutput.Count -eq 0 -or ($deleteOutput | Where-Object { $_ -match 'error|Error|ERROR' }).Count -eq 0)) {
        Write-Host "  [OK] DELETADO com sucesso!" -ForegroundColor Green
        $deletados++
    } else {
        Write-Host "  [ERRO] Falha ao deletar!" -ForegroundColor Red
        $deleteOutput | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        $erros++
    }
    
    Write-Host ""
    Start-Sleep -Milliseconds 100
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Resumo:" -ForegroundColor Cyan
Write-Host "  Total processados: $($registros.Count)" -ForegroundColor White
Write-Host "  Deletados: $deletados" -ForegroundColor Green
Write-Host "  Erros: $erros" -ForegroundColor $(if ($erros -eq 0) { "Green" } else { "Red" })
Write-Host "========================================" -ForegroundColor Cyan

