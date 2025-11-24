# Script para listar CID10 - Busca simples sem duplicatas
# Apenas mostra os registros da tabela RL_PROCEDIMENTO_CID

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$Limit = 50
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Listar CID10 - Busca Simples" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host "Limite: $Limit registros" -ForegroundColor Gray
Write-Host ""

# SQL simples - apenas SELECT direto
$sql = @"
SELECT 
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    SUBSTRING(pc.NO_CID FROM 1 FOR 60) AS NO_CID
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '$Competencia'
ORDER BY pc.INDICE
ROWS 1 TO $Limit;
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sql, [System.Text.Encoding]::ASCII)

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Buscando registros..." -ForegroundColor Yellow
Write-Host ""

# Executar
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1

# Mostrar resultado
Write-Host "Resultado:" -ForegroundColor Cyan
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

