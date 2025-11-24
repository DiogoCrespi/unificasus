# Script para verificar competencia 202510 em TODAS as tabelas

param(
    [string]$Competencia = "202510",
    [string]$DatabasePath = "",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== VERIFICACAO COMPLETA - COMPETENCIA $Competencia ===" -ForegroundColor Cyan
Write-Host ""

# Ler configuracao
if ([string]::IsNullOrEmpty($DatabasePath)) {
    $configPath = "C:\Program Files\claupers\unificasus\unificasus.ini"
    if (-not (Test-Path $configPath)) {
        Write-Host "ERRO: Arquivo de configuracao nao encontrado" -ForegroundColor Red
        exit 1
    }
    $inDbSection = $false
    Get-Content $configPath | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "[DB]") { $inDbSection = $true; return }
        if ($line.StartsWith("[") -and $inDbSection) { return }
        if ($inDbSection -and $line.StartsWith("local=", [System.StringComparison]::OrdinalIgnoreCase)) {
            $DatabasePath = $line.Substring(6).Trim()
        }
    }
}

if (-not (Test-Path $IsqlPath)) {
    $isqlCommand = Get-Command isql -ErrorAction SilentlyContinue
    if ($isqlCommand) { $IsqlPath = $isqlCommand.Source }
    else {
        Write-Host "ERRO: isql nao encontrado" -ForegroundColor Red
        exit 1
    }
}

# Tabelas para verificar
$tabelas = @("TB_PROCEDIMENTO", "TB_RUBRICA", "TB_CID", "TB_GRUPO", "TB_SUB_GRUPO", "TB_FINANCIAMENTO", 
             "TB_MODALIDADE", "TB_REGISTRO", "TB_TIPO_LEITO", "TB_SERVICO", "TB_DETALHE")

$sqlContent = "-- Verificacao de competencia $Competencia em todas as tabelas`n`n"

foreach ($tabela in $tabelas) {
    $sqlContent += "-- $tabela`n"
    $sqlContent += "SELECT '$tabela' AS TABELA, COUNT(*) AS TOTAL`n"
    $sqlContent += "FROM $tabela`n"
    $sqlContent += "WHERE DT_COMPETENCIA = '$Competencia';`n`n"
}

$sqlContent += "EXIT;`n"

$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "Executando verificacao..." -ForegroundColor Green
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
Write-Host "=== FIM ===" -ForegroundColor Cyan

