param(
    [Parameter(Mandatory = $true)]
    [string]$DatabasePath,

    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",

    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_4_0\isql.exe",

    [string]$SqlFile = "$PSScriptRoot\ajustar_colunas_import.sql",

    [string]$LogDirectory = "$PSScriptRoot\logs"
)

Write-Host "== Ajuste seguro de colunas (banco real) ==" -ForegroundColor Cyan

if (-not (Test-Path $DatabasePath)) {
    throw "Banco não encontrado: $DatabasePath"
}

if (-not (Test-Path $IsqlPath)) {
    throw "isql.exe não encontrado em: $IsqlPath"
}

if (-not (Test-Path $SqlFile)) {
    throw "Arquivo SQL não encontrado: $SqlFile"
}

if (-not (Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logFile = Join-Path $LogDirectory "ajuste_colunas_$timestamp.log"

Write-Host "Banco.............: $DatabasePath"
Write-Host "Usuário...........: $User"
Write-Host "Arquivo SQL.......: $SqlFile"
Write-Host "Log...............: $logFile"
Write-Host ""
Write-Host ">>> ANTES DE CONTINUAR: execute um backup do banco real." -ForegroundColor Yellow
Write-Host "Pressione Ctrl+C para abortar ou Enter para continuar..."
[void][System.Console]::ReadLine()

$arguments = @(
    "-user", $User,
    "-password", $Password,
    "-database", "`"$DatabasePath`"",
    "-i", "`"$SqlFile`""
)

Write-Host "Executando isql..." -ForegroundColor Cyan

& $IsqlPath @arguments *>&1 | Tee-Object -FilePath $logFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "Ajuste concluído com sucesso." -ForegroundColor Green
} else {
    Write-Host "isql retornou código $LASTEXITCODE. Revise o log." -ForegroundColor Red
    exit $LASTEXITCODE
}

