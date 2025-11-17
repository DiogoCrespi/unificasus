# Script para verificar o charset do banco Firebird
# Conecta ao banco e verifica qual charset está sendo usado

$firebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$isqlPath = Join-Path $firebirdPath "isql.exe"
$bancoPath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"

if (-not (Test-Path $isqlPath)) {
    Write-Host "ERRO: isql.exe nao encontrado em: $isqlPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VERIFICAR CHARSET DO BANCO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Script SQL para verificar charset
$sqlScript = @"
SET TERM ^;
EXECUTE BLOCK
AS
BEGIN
    -- Verifica charset do banco
    FOR SELECT RDB`$CHARACTER_SET_NAME 
        FROM RDB`$DATABASE
        INTO :charset
    DO
        SUSPEND;
END^
SET TERM ;^
"@

# Verifica charset do campo NO_PROCEDIMENTO
$sqlScript2 = @"
SELECT 
    CS.RDB`$CHARACTER_SET_NAME AS CHARSET_CAMPO
FROM RDB`$RELATION_FIELDS RF
JOIN RDB`$FIELDS F ON RF.RDB`$FIELD_SOURCE = F.RDB`$FIELD_NAME
LEFT JOIN RDB`$CHARACTER_SETS CS ON F.RDB`$CHARACTER_SET_ID = CS.RDB`$CHARACTER_SET_ID
WHERE RF.RDB`$RELATION_NAME = 'TB_PROCEDIMENTO'
  AND RF.RDB`$FIELD_NAME = 'NO_PROCEDIMENTO';
"@

# Testa leitura de um registro com acentuação
$sqlScript3 = @"
SELECT FIRST 1
    CO_PROCEDIMENTO,
    NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO
WHERE NO_PROCEDIMENTO CONTAINING 'ORIENTA'
   OR NO_PROCEDIMENTO CONTAINING 'ATEN';
"@

Write-Host "Conectando ao banco..." -ForegroundColor Cyan
Write-Host "Banco: $bancoPath" -ForegroundColor White
Write-Host ""

# Executa verificação de charset do banco
Write-Host "[1/3] Verificando charset padrao do banco..." -ForegroundColor Cyan
$result1 = & $isqlPath -user SYSDBA -password masterkey $bancoPath -i - <<< "SELECT RDB`$CHARACTER_SET_NAME FROM RDB`$DATABASE;"
Write-Host $result1

# Executa verificação de charset do campo
Write-Host ""
Write-Host "[2/3] Verificando charset do campo NO_PROCEDIMENTO..." -ForegroundColor Cyan
$result2 = & $isqlPath -user SYSDBA -password masterkey $bancoPath -i - <<< $sqlScript2
Write-Host $result2

# Testa leitura de registro
Write-Host ""
Write-Host "[3/3] Testando leitura de registro com acentuacao..." -ForegroundColor Cyan
$result3 = & $isqlPath -user SYSDBA -password masterkey $bancoPath -i - <<< $sqlScript3
Write-Host $result3

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  VERIFICACAO CONCLUIDA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

