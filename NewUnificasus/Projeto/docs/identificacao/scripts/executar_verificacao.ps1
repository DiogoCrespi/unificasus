# Script PowerShell: Executar Verificação de Tabela
# Objetivo: Facilitar a execução de scripts SQL para identificação de entidades
# Uso: .\executar_verificacao.ps1 -ScriptNome "verificar_estrutura_tabela.sql" -Tabela "TB_CID"

param(
    [Parameter(Mandatory=$true)]
    [string]$ScriptNome,
    
    [Parameter(Mandatory=$false)]
    [string]$Tabela = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Termo = "",
    
    [Parameter(Mandatory=$false)]
    [string]$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB",
    
    [Parameter(Mandatory=$false)]
    [string]$User = "SYSDBA",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "masterkey"
)

$IsqlPath = Join-Path $FirebirdPath "isql.exe"

if (-not (Test-Path $IsqlPath)) {
    Write-Host "Erro: isql.exe não encontrado em $FirebirdPath" -ForegroundColor Red
    exit 1
}

$ScriptPath = Join-Path $PSScriptRoot $ScriptNome

if (-not (Test-Path $ScriptPath)) {
    Write-Host "Erro: Script não encontrado: $ScriptPath" -ForegroundColor Red
    exit 1
}

# Criar arquivo temporário com substituições
$tempFile = [System.IO.Path]::GetTempFileName()
$scriptContent = Get-Content $ScriptPath -Raw -Encoding UTF8

# Substituir placeholders
if ($Tabela -ne "") {
    $scriptContent = $scriptContent -replace 'NOME_DA_TABELA', $Tabela
}

if ($Termo -ne "") {
    $scriptContent = $scriptContent -replace 'TERMO', $Termo
}

# Salvar arquivo temporário
$scriptContent | Out-File -FilePath $tempFile -Encoding ASCII -NoNewline

Write-Host "Executando script: $ScriptNome" -ForegroundColor Cyan
if ($Tabela -ne "") {
    Write-Host "Tabela: $Tabela" -ForegroundColor Cyan
}
if ($Termo -ne "") {
    Write-Host "Termo: $Termo" -ForegroundColor Cyan
}
Write-Host ""

# Executar script
$result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1

# Exibir resultado
Write-Host $result

# Salvar resultado em arquivo
$outputFile = Join-Path $PSScriptRoot "resultado_$([System.IO.Path]::GetFileNameWithoutExtension($ScriptNome))_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
$result | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host ""
Write-Host "Resultado salvo em: $outputFile" -ForegroundColor Green

# Limpar arquivo temporário
Remove-Item $tempFile -Force

