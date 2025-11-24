# Script para executar verificações de CID10 e uso de UPPER no banco

param(
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$scriptsPath = Join-Path $PSScriptRoot "."

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificação de CID10 e UPPER no Banco" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar estrutura de CID10
Write-Host "1. Verificando estrutura de TB_CID..." -ForegroundColor Yellow
$sqlFile = Join-Path $scriptsPath "verificar_estrutura_cid.sql"
if (Test-Path $sqlFile) {
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "   Executando: verificar_estrutura_cid.sql" -ForegroundColor Gray
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 100
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "   Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

# 2. Listar uso de UPPER no banco
Write-Host "2. Listando uso de UPPER no banco..." -ForegroundColor Yellow
$sqlFile = Join-Path $scriptsPath "listar_uso_upper_banco.sql"
if (Test-Path $sqlFile) {
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "   Executando: listar_uso_upper_banco.sql" -ForegroundColor Gray
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 100
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "   Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

# 3. Verificar uso de UPPER no código
Write-Host "3. Verificando queries dinâmicas..." -ForegroundColor Yellow
$sqlFile = Join-Path $scriptsPath "verificar_uso_upper_codigo.sql"
if (Test-Path $sqlFile) {
    $content = [System.IO.File]::ReadAllText($sqlFile, [System.Text.Encoding]::UTF8)
    $content = $content.TrimStart([char]0xFEFF)
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    [System.IO.File]::WriteAllText($tempFile, $content, [System.Text.Encoding]::ASCII)
    
    Write-Host "   Executando: verificar_uso_upper_codigo.sql" -ForegroundColor Gray
    & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1 | Select-Object -First 100
    Remove-Item $tempFile
    Write-Host ""
} else {
    Write-Host "   Arquivo não encontrado: $sqlFile" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verificação concluída!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

