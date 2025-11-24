# Script PowerShell: Executar Correção da Competência 202510
# Objetivo: Executar automaticamente o script SQL para corrigir a competência 202510
# Uso: .\executar_correcao_202510.ps1

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
        
        # Verifica se entrou na seção [DB]
        if ($trimmedLine -eq "[DB]") {
            $inDbSection = $true
            continue
        }
        
        # Se saiu da seção [DB], para de procurar
        if ($trimmedLine.StartsWith("[") -and $inDbSection) {
            break
        }
        
        # Ignora linhas comentadas ou vazias
        if ([string]::IsNullOrWhiteSpace($trimmedLine) -or $trimmedLine.StartsWith(";")) {
            continue
        }
        
        # Procura pela chave "local="
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
    Write-Host "Verifique se o arquivo $ConfigFile contém a seção [DB] com a chave 'local='" -ForegroundColor Yellow
    exit 1
}

Write-Host "Caminho do banco: $DatabasePath" -ForegroundColor Cyan

# Verificar se isql.exe existe
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

if (-not (Test-Path $IsqlPath)) {
    Write-Host "Erro: isql.exe não encontrado em $FirebirdPath" -ForegroundColor Red
    Write-Host "Verifique se o Firebird está instalado no caminho especificado." -ForegroundColor Yellow
    exit 1
}

# Caminho do script SQL
$ScriptPath = Join-Path $PSScriptRoot "EXECUTAR_CORRECAO_202510.sql"

if (-not (Test-Path $ScriptPath)) {
    Write-Host "Erro: Script SQL não encontrado: $ScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "CORREÇÃO DA COMPETÊNCIA 202510" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Banco de dados: $DatabasePath" -ForegroundColor White
Write-Host "Script SQL: $ScriptPath" -ForegroundColor White
Write-Host ""
Write-Host "Executando script..." -ForegroundColor Yellow
Write-Host ""

# Executar script
try {
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $ScriptPath 2>&1
    
    # Exibir resultado
    Write-Host $result
    
    # Salvar resultado em arquivo
    $outputFile = Join-Path $PSScriptRoot "resultado_correcao_202510_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
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
    Write-Host "2. Reinicie a aplicação UnificaSUS" -ForegroundColor White
    Write-Host "3. Verifique se a competência 202510 aparece no ComboBox" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "Erro ao executar script: $_" -ForegroundColor Red
    exit 1
}

