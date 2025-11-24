# Script para mover arquivos de limpeza para subpasta "temp 2"

$scriptsPath = $PSScriptRoot
$tempDir = Join-Path $scriptsPath "temp 2"

# Criar pasta se não existir
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    Write-Host "Pasta criada: temp 2" -ForegroundColor Green
} else {
    Write-Host "Pasta ja existe: temp 2" -ForegroundColor Yellow
}

# Arquivos para mover
$files = @(
    "limpar_duplicatas_com_logs_detalhados.sql",
    "listar_duplicatas_antes.sql",
    "executar_limpeza_servidor.bat",
    "check_duplicatas.sql",
    "limpar_duplicatas_servidor.sql",
    "limpar_duplicatas_listar_primeiro.sql",
    "copiar_para_servidor.ps1",
    "executar_limpeza_remoto.ps1",
    "executar_limpeza_ssh.ps1"
)

Write-Host ""
Write-Host "Movendo arquivos para 'temp 2'..." -ForegroundColor Cyan
Write-Host ""

$movedCount = 0
$notFoundCount = 0

foreach ($file in $files) {
    $source = Join-Path $scriptsPath $file
    if (Test-Path $source) {
        $dest = Join-Path $tempDir $file
        Move-Item -Path $source -Destination $dest -Force
        Write-Host "  [OK] Movido: $file" -ForegroundColor Green
        $movedCount++
    } else {
        Write-Host "  [AVISO] Nao encontrado: $file" -ForegroundColor Yellow
        $notFoundCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resumo:" -ForegroundColor Cyan
Write-Host "  Movidos: $movedCount arquivos" -ForegroundColor Green
Write-Host "  Nao encontrados: $notFoundCount arquivos" -ForegroundColor $(if ($notFoundCount -eq 0) { "Green" } else { "Yellow" })
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pasta destino: $tempDir" -ForegroundColor Gray

