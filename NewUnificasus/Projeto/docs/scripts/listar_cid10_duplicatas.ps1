# Script para listar CID10 DUPLICADOS via PowerShell
# Execute no servidor (192.168.0.3)

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$Limit = 20
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Listar CID10 DUPLICADOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host "Limite: $Limit grupos duplicados" -ForegroundColor Gray
Write-Host ""

# Criar script SQL temporário
$sqlContent = @"
-- Listar grupos de duplicatas
SELECT 
    'GRUPO DUPLICADO' AS TIPO,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    COUNT(*) AS QTD_DUPLICATAS,
    MIN(pc.INDICE) AS INDICE_MENOR,
    MAX(pc.INDICE) AS INDICE_MAIOR,
    SUBSTRING(MIN(pc.NO_CID) FROM 1 FOR 50) AS NO_CID_EXEMPLO
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '$Competencia'
GROUP BY pc.CO_CID, pc.CO_PROCEDIMENTO, pc.DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC
ROWS 1 TO $Limit;
"@

$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Buscando duplicatas..." -ForegroundColor Yellow
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

