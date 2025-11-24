# Script de Restore do Banco Firebird usando GBak

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    
    [string]$ServerHost = "192.168.0.3",
    [string]$DatabasePath = "E:\claupers\unificasus\UNIFICASUS.GDB",
    [string]$GbakPath = "C:\Program Files\Firebird\Firebird_3_0\gbak.exe"
)

$dbConnection = "$ServerHost`:$DatabasePath"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Restore do Banco Firebird" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backup: $BackupFile" -ForegroundColor Gray
Write-Host "Banco: $dbConnection" -ForegroundColor Gray
Write-Host ""

# Verificar se arquivo de backup existe
if (-not (Test-Path $BackupFile)) {
    Write-Host "[ERRO] Arquivo de backup não encontrado: $BackupFile" -ForegroundColor Red
    exit 1
}

# Verificar se gbak existe
if (-not (Test-Path $GbakPath)) {
    Write-Host "[ERRO] GBak não encontrado: $GbakPath" -ForegroundColor Red
    exit 1
}

Write-Host "[AVISO] Isso vai SUBSTITUIR o banco atual!" -ForegroundColor Yellow
Write-Host "[AVISO] Certifique-se de que não há conexões ativas." -ForegroundColor Yellow
Write-Host ""
$confirm = Read-Host "Deseja continuar? (S/N)"

if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operação cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Iniciando restore..." -ForegroundColor Yellow

# Executar restore
$process = Start-Process -FilePath $GbakPath -ArgumentList @(
    "-c",
    "-user", "SYSDBA",
    "-password", "masterkey",
    "`"$BackupFile`"",
    "`"$dbConnection`"",
    "-replace"
) -Wait -NoNewWindow -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Banco restaurado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "[ERRO] Falha ao restaurar backup (Exit Code: $($process.ExitCode))" -ForegroundColor Red
    exit 1
}

