# Script PowerShell: Executar Correção de Encoding - Competência 202510
# Objetivo: Corrigir caracteres corrompidos nos dados já importados

param(
    [Parameter(Mandatory=$false)]
    [string]$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0",
    
    [Parameter(Mandatory=$false)]
    [string]$User = "SYSDBA",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "masterkey"
)

# Caminho do arquivo de configuração
$ConfigFile = "C:\Program Files\claupers\unificasus\unificasus.ini"

# Função para ler o caminho do banco do arquivo de configuração
function Get-DatabasePath {
    if (-not (Test-Path $ConfigFile)) {
        Write-Host "Erro: Arquivo de configuração não encontrado: $ConfigFile" -ForegroundColor Red
        return $null
    }
    
    $lines = Get-Content $ConfigFile
    $inDbSection = $false
    
    foreach ($line in $lines) {
        $trimmedLine = $line.Trim()
        
        if ($trimmedLine -eq "[DB]") {
            $inDbSection = $true
            continue
        }
        
        if ($trimmedLine.StartsWith("[") -and $inDbSection) {
            break
        }
        
        if ([string]::IsNullOrWhiteSpace($trimmedLine) -or $trimmedLine.StartsWith(";")) {
            continue
        }
        
        if ($inDbSection -and $trimmedLine.StartsWith("local=", [System.StringComparison]::OrdinalIgnoreCase)) {
            $value = $trimmedLine.Substring(6).Trim()
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                return $value
            }
        }
    }
    
    return $null
}

# Obter caminho do banco
$DatabasePath = Get-DatabasePath

if ($null -eq $DatabasePath) {
    Write-Host "Erro: Não foi possível obter o caminho do banco de dados do arquivo de configuração." -ForegroundColor Red
    exit 1
}

Write-Host "Caminho do banco: $DatabasePath" -ForegroundColor Cyan

# Verificar se isql.exe existe
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

if (-not (Test-Path $IsqlPath)) {
    Write-Host "Erro: isql.exe não encontrado em $FirebirdPath" -ForegroundColor Red
    exit 1
}

# Caminho do script SQL
$ScriptPath = Join-Path $PSScriptRoot "corrigir_encoding_dados_202510.sql"

if (-not (Test-Path $ScriptPath)) {
    Write-Host "Erro: Script SQL não encontrado: $ScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "CORREÇÃO DE ENCODING - COMPETÊNCIA 202510" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco de dados: $DatabasePath" -ForegroundColor White
Write-Host "Script SQL: $ScriptPath" -ForegroundColor White
Write-Host ""
Write-Host "ATENÇÃO: Este script irá atualizar os dados já importados." -ForegroundColor Yellow
Write-Host "Deseja continuar? (S/N)" -ForegroundColor Yellow
$confirm = Read-Host

if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operação cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Executando correção..." -ForegroundColor Yellow
Write-Host ""

# Executar script
try {
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $ScriptPath 2>&1
    
    # Exibir resultado
    Write-Host $result
    
    # Salvar resultado em arquivo
    $outputFile = Join-Path $PSScriptRoot "resultado_correcao_encoding_202510_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    $result | Out-File -FilePath $outputFile -Encoding UTF8
    
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "EXECUÇÃO CONCLUÍDA" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultado salvo em: $outputFile" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Yellow
    Write-Host "1. Verifique o resultado acima" -ForegroundColor White
    Write-Host "2. Verifique se os textos foram corrigidos corretamente" -ForegroundColor White
    Write-Host "3. Se necessário, reimporte os dados com encoding correto" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "Erro ao executar script: $_" -ForegroundColor Red
    exit 1
}

