# Script para listar CID10 do banco Firebird via PowerShell
# Execute no servidor (192.168.0.3)

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510",
    [int]$Limit = 50
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Listar CID10 - Firebird" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host "Limite: $Limit registros" -ForegroundColor Gray
Write-Host ""

# Criar script SQL temporário
$sqlContent = @"
SET TERM ^ ;

-- Listar CID10 da competência
SELECT 
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
    SUBSTRING(pc.NO_CID FROM 1 FOR 60) AS NO_CID,
    CASE 
        WHEN UPPER(pc.NO_CID) = pc.NO_CID THEN 'MAIUSCULA'
        ELSE 'MISTO'
    END AS TIPO
FROM RL_PROCEDIMENTO_CID pc
WHERE pc.DT_COMPETENCIA = '$Competencia'
ORDER BY pc.INDICE
ROWS 1 TO $Limit
^

SET TERM ; ^
"@

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

