# Script para listar DETALHES de uma duplicata específica
# Execute no servidor (192.168.0.3)

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [Parameter(Mandatory=$false)]
    [string]$CoCID = "",
    [Parameter(Mandatory=$false)]
    [string]$CoProcedimento = ""
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Detalhes de Duplicata CID10" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrEmpty($CoCID) -or [string]::IsNullOrEmpty($CoProcedimento)) {
    Write-Host "[INFO] Use: .\listar_detalhes_duplicata.ps1 -CoCID 'I10' -CoProcedimento '0301010030'" -ForegroundColor Yellow
    Write-Host "[INFO] Ou execute sem parâmetros para ver exemplos" -ForegroundColor Yellow
    Write-Host ""
    
    # Mostrar um exemplo
    $sqlContent = @"
-- Exemplo: Primeiro grupo de duplicatas
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
ORDER BY pc.CO_CID, pc.CO_PROCEDIMENTO, pc.INDICE
ROWS 1 TO 10;
"@
} else {
    Write-Host "CID: $CoCID" -ForegroundColor Gray
    Write-Host "Procedimento: $CoProcedimento" -ForegroundColor Gray
    Write-Host "Competencia: $Competencia" -ForegroundColor Gray
    Write-Host ""
    
    $sqlContent = @"
-- Detalhes da duplicata específica
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
  AND pc.CO_CID = '$CoCID'
  AND pc.CO_PROCEDIMENTO = '$CoProcedimento'
ORDER BY pc.INDICE;
"@
}

$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Executando query..." -ForegroundColor Yellow
Write-Host ""

# Executar via isql
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSqlFile" -q 2>&1

# Mostrar resultado
$output | ForEach-Object {
    Write-Host $_ -ForegroundColor White
}

# Limpar arquivo temporário
Remove-Item $tempSqlFile -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Concluído!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

