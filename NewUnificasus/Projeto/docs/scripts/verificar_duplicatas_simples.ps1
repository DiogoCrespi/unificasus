# Script para verificar duplicatas de forma simples
# Agrupa por CO_CID + CO_PROCEDIMENTO + DT_COMPETENCIA

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$Limit = 20
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificar Duplicatas - Busca Simples" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host ""

# SQL simples - agrupa e conta
$sql = @"
SELECT 
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS QTD
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '$Competencia'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC
ROWS 1 TO $Limit;
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sql, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Buscando grupos duplicados..." -ForegroundColor Yellow
Write-Host ""

# Executar
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1

# Mostrar resultado
Write-Host "Grupos com duplicatas:" -ForegroundColor Cyan
Write-Host "----------------------------------------" -ForegroundColor Gray
$output | ForEach-Object {
    Write-Host $_ -ForegroundColor White
}
Write-Host "----------------------------------------" -ForegroundColor Gray

# Limpar
Remove-Item $tempSql -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Concluido!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

