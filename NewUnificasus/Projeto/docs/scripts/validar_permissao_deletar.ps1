# Script para validar permissões de DELETE no banco Firebird
# Identifica o motivo pelo qual não consegue deletar

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$Competencia = "202510"
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Validacao de Permissoes - DELETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Usuario: SYSDBA" -ForegroundColor Gray
Write-Host ""

# 1. Testar conexão
Write-Host "[1/6] Testando conexao..." -ForegroundColor Yellow
$sqlTest = "SELECT 1 FROM RDB`$DATABASE;"
$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlTest, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Conexao estabelecida" -ForegroundColor Green
} else {
    Write-Host "  [ERRO] Falha na conexao!" -ForegroundColor Red
    $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    exit 1
}

Write-Host ""

# 2. Verificar se a tabela existe
Write-Host "[2/6] Verificando se tabela RL_PROCEDIMENTO_CID existe..." -ForegroundColor Yellow
$sqlTable = "SELECT COUNT(*) FROM RDB`$RELATIONS WHERE RDB`$RELATION_NAME = 'RL_PROCEDIMENTO_CID';"
$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlTable, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

if ($output -match '^\s*1\s*$') {
    Write-Host "  [OK] Tabela existe" -ForegroundColor Green
} else {
    Write-Host "  [ERRO] Tabela nao encontrada!" -ForegroundColor Red
    $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
}

Write-Host ""

# 3. Verificar permissões do usuário
Write-Host "[3/6] Verificando permissoes do usuario SYSDBA..." -ForegroundColor Yellow
$sqlPerm = @"
SELECT 
    RDB`$USER AS USUARIO,
    RDB`$RELATION_NAME AS TABELA,
    RDB`$PRIVILEGE AS PRIVILEGIO
FROM RDB`$USER_PRIVILEGES
WHERE RDB`$RELATION_NAME = 'RL_PROCEDIMENTO_CID'
  AND RDB`$USER = 'SYSDBA';
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlPerm, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

Write-Host "  Permissoes encontradas:" -ForegroundColor Gray
$output | Where-Object { $_ -match 'SYSDBA|RL_PROCEDIMENTO_CID|D|S|I|U|R' } | ForEach-Object {
    Write-Host "    $_" -ForegroundColor White
}

if ($output -match 'D|DELETE') {
    Write-Host "  [OK] Permissao DELETE encontrada" -ForegroundColor Green
} else {
    Write-Host "  [AVISO] Permissao DELETE nao encontrada explicitamente (SYSDBA tem todas as permissoes)" -ForegroundColor Yellow
}

Write-Host ""

# 4. Contar registros duplicados
Write-Host "[4/6] Contando registros duplicados..." -ForegroundColor Yellow
$sqlCount = @"
SELECT COUNT(*) AS TOTAL
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
  );
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlCount, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

$count = 0
if ($output -match '^\s*(\d+)\s*$') {
    $count = [int]$Matches[1]
    Write-Host "  [OK] Encontrados $count registros duplicados para deletar" -ForegroundColor Green
} else {
    Write-Host "  [AVISO] Nao foi possivel contar registros" -ForegroundColor Yellow
    $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
}

Write-Host ""

# 5. Testar SELECT em um registro específico
Write-Host "[5/6] Testando SELECT em um registro..." -ForegroundColor Yellow
$sqlSelect = @"
SELECT FIRST 1
    pc.INDICE,
    pc.CO_CID,
    pc.CO_PROCEDIMENTO,
    pc.DT_COMPETENCIA
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
[System.IO.File]::WriteAllText($tempSql, $sqlSelect, [System.Text.Encoding]::ASCII)

$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
Remove-Item $tempSql -ErrorAction SilentlyContinue

$testIndice = $null
$testCoCid = $null
$testCoProc = $null

foreach ($linha in $output) {
    if ($linha -match '^\s*(\d+)\s+([A-Z0-9]+)\s+([A-Z0-9]+)\s+(\d{6})') {
        $testIndice = $Matches[1]
        $testCoCid = $Matches[2]
        $testCoProc = $Matches[3]
        break
    }
}

if ($testIndice) {
    Write-Host "  [OK] SELECT funcionou - Registro encontrado:" -ForegroundColor Green
    Write-Host "    INDICE: $testIndice" -ForegroundColor White
    Write-Host "    CO_CID: $testCoCid" -ForegroundColor White
    Write-Host "    CO_PROCEDIMENTO: $testCoProc" -ForegroundColor White
} else {
    Write-Host "  [ERRO] SELECT nao retornou resultados!" -ForegroundColor Red
    $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "  [INFO] Possiveis motivos:" -ForegroundColor Yellow
    Write-Host "    - Nao ha registros duplicados" -ForegroundColor Yellow
    Write-Host "    - Query esta incorreta" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# 6. Testar DELETE real
Write-Host "[6/6] Testando DELETE no registro encontrado..." -ForegroundColor Yellow
Write-Host "  [AVISO] Vou tentar deletar o registro de teste" -ForegroundColor Yellow

$sqlDelete = @"
DELETE FROM RL_PROCEDIMENTO_CID
WHERE INDICE = $testIndice
  AND DT_COMPETENCIA = '$Competencia'
  AND CO_CID = '$testCoCid'
  AND CO_PROCEDIMENTO = '$testCoProc';
"@

$tempSql = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSql, $sqlDelete, [System.Text.Encoding]::ASCII)

Write-Host "  Executando DELETE..." -ForegroundColor Gray
$output = & "$IsqlPath" -user SYSDBA -password masterkey "$dbConnection" -i "$tempSql" -q 2>&1
$exitCode = $LASTEXITCODE
Remove-Item $tempSql -ErrorAction SilentlyContinue

Write-Host ""
if ($exitCode -eq 0 -and ($output -eq $null -or $output.Count -eq 0 -or ($output | Where-Object { $_ -match 'error|Error|ERROR|violat|constraint|trigger' }).Count -eq 0)) {
    Write-Host "  [OK] DELETE executado com sucesso!" -ForegroundColor Green
    Write-Host "  [INFO] Permissao DELETE esta OK" -ForegroundColor Green
} else {
    Write-Host "  [ERRO] DELETE falhou!" -ForegroundColor Red
    Write-Host "  Detalhes do erro:" -ForegroundColor Red
    $output | ForEach-Object { 
        Write-Host "    $_" -ForegroundColor Red 
    }
    Write-Host ""
    Write-Host "  [INFO] Possiveis motivos:" -ForegroundColor Yellow
    
    if ($output -match 'violat|constraint|foreign key') {
        Write-Host "    - Existe constraint de chave estrangeira" -ForegroundColor Yellow
        Write-Host "    - Outra tabela referencia este registro" -ForegroundColor Yellow
    }
    if ($output -match 'trigger') {
        Write-Host "    - Trigger bloqueando a exclusao" -ForegroundColor Yellow
    }
    if ($output -match 'permission|privilege|access') {
        Write-Host "    - Sem permissao para deletar" -ForegroundColor Yellow
    }
    if ($output -match 'lock|transaction') {
        Write-Host "    - Registro esta bloqueado por outra transacao" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Validacao concluida!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

