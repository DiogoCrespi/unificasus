# Script para testar deletar APENAS UM registro duplicado
# Versão simples para diagnosticar o problema

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510"
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Teste: Deletar UM Registro Duplicado" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Competencia: $Competencia" -ForegroundColor Gray
Write-Host ""

# 1. Buscar UM registro duplicado
Write-Host "[1/3] Buscando UM registro duplicado..." -ForegroundColor Yellow

$sqlBuscar = @"
SELECT FIRST 1
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA,
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

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlBuscar, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

# Parsear resultado
$indice = $null
$coCid = $null
$coProcedimento = $null
$noCid = $null

Write-Host "  Output do SELECT:" -ForegroundColor Gray
$output | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }

foreach ($linha in $output) {
    # Tentar diferentes formatos de parse
    if ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)\s+(\d{6})\s+(.+)$') {
        $indice = $Matches[1]
        $coCid = $Matches[2]
        $coProcedimento = $Matches[3]
        $noCid = $Matches[5].Trim()
        break
    } elseif ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)') {
        $indice = $Matches[1]
        $coCid = $Matches[2]
        $coProcedimento = $Matches[3]
        break
    }
}

if (-not $indice) {
    Write-Host ""
    Write-Host "[ERRO] Nenhum registro duplicado encontrado!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Output completo:" -ForegroundColor Yellow
    $output | ForEach-Object { Write-Host $_ -ForegroundColor White }
    exit 1
}

Write-Host ""
Write-Host "[OK] Registro encontrado:" -ForegroundColor Green
Write-Host "  INDICE: $indice" -ForegroundColor White
Write-Host "  CO_CID: $coCid" -ForegroundColor White
Write-Host "  CO_PROCEDIMENTO: $coProcedimento" -ForegroundColor White
Write-Host "  DT_COMPETENCIA: $Competencia" -ForegroundColor White
if ($noCid) {
    Write-Host "  NO_CID: $noCid" -ForegroundColor White
}
Write-Host ""

# 2. Verificar se o registro realmente existe
Write-Host "[2/3] Verificando se registro existe antes de deletar..." -ForegroundColor Yellow

$sqlVerificar = @"
SELECT COUNT(*) AS EXISTE
FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $indice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$coCid'
  AND CO_PROCEDIMENTO = '$coProcedimento';
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlVerificar, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

$existe = $false
if ($output -match '^\s*1\s*$') {
    $existe = $true
    Write-Host "  [OK] Registro existe no banco" -ForegroundColor Green
} else {
    Write-Host "  [ERRO] Registro nao encontrado no banco!" -ForegroundColor Red
    $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    exit 1
}

Write-Host ""

# 3. Tentar deletar
Write-Host "[3/3] Tentando deletar o registro..." -ForegroundColor Yellow
Write-Host ""

$sqlDelete = @"
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $indice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$coCid'
  AND CO_PROCEDIMENTO = '$coProcedimento';
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlDelete, [System.Text.Encoding]::ASCII)

Write-Host "  SQL a executar:" -ForegroundColor Gray
Write-Host "  $sqlDelete" -ForegroundColor DarkGray
Write-Host ""

Write-Host "  Executando DELETE..." -ForegroundColor Yellow
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" 2>&1
$exitCode = $LASTEXITCODE
Remove-Item $tempSql -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "  Exit Code: $exitCode" -ForegroundColor Gray
Write-Host "  Output completo:" -ForegroundColor Gray
$output | ForEach-Object { Write-Host "    $_" -ForegroundColor White }

Write-Host ""

# Verificar se deletou
if ($exitCode -eq 0) {
    # Verificar se o registro ainda existe
    $sqlVerificar2 = @"
SELECT COUNT(*) AS EXISTE
FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $indice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$coCid'
  AND CO_PROCEDIMENTO = '$coProcedimento';
"@
    
    $tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempSql, $sqlVerificar2, [System.Text.Encoding]::ASCII)
    
    $output2 = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
    Remove-Item $tempSql -ErrorAction SilentlyContinue
    
    if ($output2 -match '^\s*0\s*$') {
        Write-Host "  [OK] SUCESSO! Registro foi deletado!" -ForegroundColor Green
        Write-Host "  [INFO] DELETE funcionou corretamente" -ForegroundColor Green
    } else {
        Write-Host "  [AVISO] DELETE executou mas registro ainda existe" -ForegroundColor Yellow
        Write-Host "  [INFO] Pode haver trigger ou constraint impedindo" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [ERRO] DELETE falhou!" -ForegroundColor Red
    
    # Analisar o erro
    $erro = $output -join " "
    
    if ($erro -match 'violat|constraint|foreign key|referential') {
        Write-Host ""
        Write-Host "  [DIAGNOSTICO] Erro de constraint/chave estrangeira" -ForegroundColor Yellow
        Write-Host "    - Outra tabela referencia este registro" -ForegroundColor Yellow
        Write-Host "    - Precisa deletar referencias primeiro" -ForegroundColor Yellow
    }
    elseif ($erro -match 'trigger') {
        Write-Host ""
        Write-Host "  [DIAGNOSTICO] Erro de trigger" -ForegroundColor Yellow
        Write-Host "    - Trigger esta bloqueando a exclusao" -ForegroundColor Yellow
    }
    elseif ($erro -match 'permission|privilege|access|denied') {
        Write-Host ""
        Write-Host "  [DIAGNOSTICO] Erro de permissao" -ForegroundColor Yellow
        Write-Host "    - Usuario nao tem permissao para deletar" -ForegroundColor Yellow
    }
    elseif ($erro -match 'lock|transaction|deadlock') {
        Write-Host ""
        Write-Host "  [DIAGNOSTICO] Erro de lock/transacao" -ForegroundColor Yellow
        Write-Host "    - Registro esta bloqueado por outra transacao" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "  [DIAGNOSTICO] Erro desconhecido" -ForegroundColor Yellow
        Write-Host "    - Verifique a mensagem de erro acima" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Teste concluido!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

