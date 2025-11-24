# Script para verificar se a competencia foi registrada e aparece na listagem

param(
    [string]$Competencia = "202510",
    [string]$DatabasePath = "",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== VERIFICACAO DE COMPETENCIA ===" -ForegroundColor Cyan
Write-Host "Competencia: $Competencia" -ForegroundColor Yellow
Write-Host ""

# 1. Ler configuracao do banco se nao foi fornecida
if ([string]::IsNullOrEmpty($DatabasePath)) {
    Write-Host "[1/4] Lendo configuracao do banco..." -ForegroundColor Green
    $configPath = "C:\Program Files\claupers\unificasus\unificasus.ini"

    if (-not (Test-Path $configPath)) {
        Write-Host "ERRO: Arquivo de configuracao nao encontrado: $configPath" -ForegroundColor Red
        exit 1
    }

    $inDbSection = $false
    Get-Content $configPath | ForEach-Object {
        $line = $_.Trim()
        
        if ($line -eq "[DB]") {
            $inDbSection = $true
            return
        }
        
        if ($line.StartsWith("[") -and $inDbSection) {
            return
        }
        
        if ($inDbSection -and $line.StartsWith("local=", [System.StringComparison]::OrdinalIgnoreCase)) {
            $DatabasePath = $line.Substring(6).Trim()
        }
    }

    if ([string]::IsNullOrEmpty($DatabasePath)) {
        Write-Host "ERRO: Nao foi possivel encontrar o caminho do banco" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Caminho do banco: $DatabasePath" -ForegroundColor Gray
} else {
    Write-Host "[1/4] Usando caminho fornecido: $DatabasePath" -ForegroundColor Green
}

Write-Host ""

# 2. Verificar isql
Write-Host "[2/4] Verificando isql..." -ForegroundColor Green

if (-not (Test-Path $IsqlPath)) {
    $isqlCommand = Get-Command isql -ErrorAction SilentlyContinue
    if ($isqlCommand) {
        $IsqlPath = $isqlCommand.Source
    } else {
        Write-Host "ERRO: isql nao encontrado" -ForegroundColor Red
        exit 1
    }
}

Write-Host "  isql: $IsqlPath" -ForegroundColor Gray
Write-Host ""

# 3. Criar script SQL
Write-Host "[3/4] Criando script SQL..." -ForegroundColor Green

$sqlContent = @"
-- Verificacao de competencia $Competencia

-- 0. Verificar se TB_COMPETENCIA_ATIVA existe
SELECT 'VERIFICACAO: Tabela TB_COMPETENCIA_ATIVA existe?' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT COUNT(*) AS existe
FROM RDB`$RELATIONS
WHERE RDB`$RELATION_NAME = 'TB_COMPETENCIA_ATIVA'
  AND RDB`$SYSTEM_FLAG = 0;

-- 1. Verificar se existe em TB_PROCEDIMENTO
SELECT 'TB_PROCEDIMENTO: Verificando se competencia existe' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT COUNT(*) AS total_procedimentos
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '$Competencia';

-- 2. Listar TODAS as competencias em TB_PROCEDIMENTO
SELECT 'TB_PROCEDIMENTO: Todas as competencias com dados' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT DISTINCT DT_COMPETENCIA, COUNT(*) AS total
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
  AND TRIM(DT_COMPETENCIA) <> ''
GROUP BY DT_COMPETENCIA
ORDER BY DT_COMPETENCIA DESC;

EXIT;
"@

$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "  Script criado: $tempSqlFile" -ForegroundColor Gray
Write-Host ""

# 4. Executar
Write-Host "[4/4] Executando verificacoes..." -ForegroundColor Green
Write-Host ""

try {
    $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempSqlFile 2>&1
    
    Write-Host $output
    
} catch {
    Write-Host "ERRO: $_" -ForegroundColor Red
} finally {
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host ""
Write-Host "=== FIM DA VERIFICACAO ===" -ForegroundColor Cyan

