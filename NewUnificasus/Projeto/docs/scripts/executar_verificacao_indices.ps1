# Script PowerShell: Executar Verificação de Índices
# Objetivo: Executar script SQL para verificar índices existentes

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe",
    [string]$SqlFile = "verificar_indices_existentes.sql"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFilePath = Join-Path $scriptDir $SqlFile

if (-not (Test-Path $sqlFilePath)) {
    Write-Host "Erro: Arquivo SQL não encontrado: $sqlFilePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $IsqlPath)) {
    Write-Host "Erro: isql.exe não encontrado em: $IsqlPath" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificando Índices no Banco de Dados" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Yellow
Write-Host "Arquivo SQL: $sqlFilePath" -ForegroundColor Yellow
Write-Host ""

# Ler conteúdo do arquivo SQL
$sqlContent = Get-Content $sqlFilePath -Raw -Encoding UTF8

# Criar arquivo temporário
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
Set-Content -Path $tempFile -Value $sqlContent -Encoding UTF8

try {
    # Executar script SQL
    Write-Host "Executando verificação de índices..." -ForegroundColor Green
    
    $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    Write-Host ""
    Write-Host "Resultado:" -ForegroundColor Cyan
    Write-Host $output
    
    Write-Host ""
    Write-Host "Verificação concluída!" -ForegroundColor Green
}
catch {
    Write-Host "Erro ao executar script: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Remover arquivo temporário
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }
}

