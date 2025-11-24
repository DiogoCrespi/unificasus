# Script de Substituição Direta do Banco (RÁPIDO, mas arriscado)
# ATENÇÃO: Só use se tiver certeza de que não há conexões ativas!

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$BackupDir = "E:\backup"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Substituição Direta do Banco" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[AVISO] Este método é RÁPIDO mas ARRISCADO!" -ForegroundColor Yellow
Write-Host "[AVISO] Use apenas se tiver certeza de que:" -ForegroundColor Yellow
Write-Host "  1. Nenhuma aplicação está usando o banco" -ForegroundColor Yellow
Write-Host "  2. Não há conexões ativas" -ForegroundColor Yellow
Write-Host "  3. Você tem um backup do banco atual" -ForegroundColor Yellow
Write-Host ""

# Verificar se arquivo de backup existe
if (-not (Test-Path $BackupFile)) {
    Write-Host "[ERRO] Arquivo de backup não encontrado: $BackupFile" -ForegroundColor Red
    exit 1
}

# Verificar se banco existe
if (-not (Test-Path $DatabasePath)) {
    Write-Host "[AVISO] Banco atual não encontrado: $DatabasePath" -ForegroundColor Yellow
    Write-Host "[INFO] Será criado um novo banco." -ForegroundColor Gray
}

$confirm = Read-Host "Deseja continuar? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operação cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Criando backup do banco atual..." -ForegroundColor Yellow

# Criar backup do banco atual
if (Test-Path $DatabasePath) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupAtual = Join-Path $BackupDir "UNIFICASUS_BACKUP_$timestamp.GDB"
    
    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }
    
    try {
        Copy-Item -Path $DatabasePath -Destination $backupAtual -Force
        Write-Host "[OK] Backup do banco atual criado: $backupAtual" -ForegroundColor Green
    }
    catch {
        Write-Host "[ERRO] Falha ao criar backup do banco atual: $_" -ForegroundColor Red
        Write-Host "[AVISO] Continuando mesmo assim..." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Substituindo banco..." -ForegroundColor Yellow

# Substituir banco
try {
    Copy-Item -Path $BackupFile -Destination $DatabasePath -Force
    Write-Host "[OK] Banco substituído com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "[INFO] Reinicie a aplicação para usar o novo banco." -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
}
catch {
    Write-Host "[ERRO] Falha ao substituir banco: $_" -ForegroundColor Red
    Write-Host "[AVISO] O banco pode estar em uso. Feche todas as aplicações e tente novamente." -ForegroundColor Yellow
    exit 1
}

