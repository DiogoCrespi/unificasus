# Script de Backup do Banco Firebird usando GBak

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$BackupDir = "E:\backup",
    [string]$GbakPath = "C:\Program Files\Firebird\Firebird_3_0\gbak.exe"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDir "UNIFICASUS_$timestamp.fbk"
$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Backup do Banco Firebird" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host "Backup: $backupFile" -ForegroundColor Gray
Write-Host ""

# Criar diretório de backup se não existir
if (-not (Test-Path $BackupDir)) {
    Write-Host "[INFO] Criando diretório de backup: $BackupDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

# Verificar se gbak existe
if (-not (Test-Path $GbakPath)) {
    Write-Host "[ERRO] GBak não encontrado: $GbakPath" -ForegroundColor Red
    exit 1
}

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iniciando backup..." -ForegroundColor Yellow

# Executar backup
$process = Start-Process -FilePath $GbakPath -ArgumentList @(
    "-b",
    "-user", "SYSDBA",
    "-password", "masterkey",
    "`"$dbConnection`"",
    "`"$backupFile`""
) -Wait -NoNewWindow -PassThru

if ($process.ExitCode -eq 0) {
    $fileSize = (Get-Item $backupFile).Length / 1MB
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Backup criado com sucesso!" -ForegroundColor Green
    Write-Host "  Arquivo: $backupFile" -ForegroundColor Gray
    Write-Host "  Tamanho: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Gray
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "[ERRO] Falha ao criar backup (Exit Code: $($process.ExitCode))" -ForegroundColor Red
    exit 1
}

