# Script PowerShell para executar verificação das tabelas de Serviço e Classificação
# Requer: Firebird instalado com isql disponível no PATH

param(
    [string]$DatabasePath = "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$OutputFile = ".\docs\scripts\resultados\resultado_verificacao_servico_classificacao_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
)

# Caminho do script SQL
$sqlScript = ".\docs\scripts\verificar_tabelas_servico_classificacao.sql"
$resultFile = $OutputFile

Write-Host "=== Verificação de Tabelas de Serviço/Classificação ===" -ForegroundColor Cyan
Write-Host "Banco de dados: $DatabasePath" -ForegroundColor Yellow
Write-Host "Script SQL: $sqlScript" -ForegroundColor Yellow
Write-Host "Arquivo de resultado: $resultFile" -ForegroundColor Yellow
Write-Host ""

# Verificar se o arquivo SQL existe
if (-not (Test-Path $sqlScript)) {
    Write-Host "ERRO: Arquivo SQL não encontrado: $sqlScript" -ForegroundColor Red
    exit 1
}

# Criar diretório de resultados se não existir
$resultDir = Split-Path -Parent $resultFile
if (-not (Test-Path $resultDir)) {
    New-Item -ItemType Directory -Path $resultDir -Force | Out-Null
    Write-Host "Diretório de resultados criado: $resultDir" -ForegroundColor Green
}

# Verificar se isql está disponível
$isqlPath = Get-Command isql -ErrorAction SilentlyContinue
if (-not $isqlPath) {
    Write-Host "ERRO: isql não encontrado no PATH. Verifique se o Firebird está instalado." -ForegroundColor Red
    exit 1
}

Write-Host "Executando script SQL..." -ForegroundColor Green

# Executar script SQL via isql
$connectionString = "$DatabasePath -user $User -password $Password"

try {
    # Ler o conteúdo do SQL
    $sqlContent = Get-Content $sqlScript -Raw -Encoding UTF8
    
    # Preparar comando isql
    # isql requer que o SQL seja passado via stdin ou arquivo
    $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $sqlContent | Out-File -FilePath $tempSqlFile -Encoding UTF8 -NoNewline
    
    # Executar isql e redirecionar output
    $result = & isql -i $tempSqlFile $connectionString 2>&1
    
    # Salvar resultado
    $result | Out-File -FilePath $resultFile -Encoding UTF8
    
    Write-Host "✓ Script executado com sucesso!" -ForegroundColor Green
    Write-Host "Resultado salvo em: $resultFile" -ForegroundColor Green
    Write-Host ""
    
    # Mostrar resumo do resultado
    Write-Host "=== RESUMO DO RESULTADO ===" -ForegroundColor Cyan
    Get-Content $resultFile | Select-Object -First 50 | Write-Host
    
    if ((Get-Content $resultFile).Count -gt 50) {
        Write-Host "... (arquivo truncado, verifique o arquivo completo)" -ForegroundColor Yellow
    }
    
    # Limpar arquivo temporário
    Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    
    Write-Host ""
    Write-Host "Verificação concluída! Verifique o arquivo de resultado para mais detalhes." -ForegroundColor Green
    
} catch {
    Write-Host "ERRO ao executar script: $_" -ForegroundColor Red
    exit 1
}

