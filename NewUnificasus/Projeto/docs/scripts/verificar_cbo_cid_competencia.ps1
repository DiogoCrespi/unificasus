# ============================================================================
# Script PowerShell para verificar se CBO, CID e descrições são separados 
# por competência ou se valem para todas as competências
# ============================================================================

# Configurações
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$User = "SYSDBA"
$Password = "masterkey"

# Tenta ler o caminho do banco do arquivo de configuração
$configFile = "C:\Program Files\claupers\unificasus\unificasus.ini"
$DatabasePath = ""

if (Test-Path $configFile) {
    $configContent = Get-Content $configFile -Raw
    if ($configContent -match '(?m)^local\s*=\s*(.+)$') {
        $DatabasePath = $matches[1].Trim()
        Write-Host "Caminho do banco lido do arquivo de configuração: $DatabasePath" -ForegroundColor Green
    }
}

# Se não encontrou, usa o padrão
if ([string]::IsNullOrEmpty($DatabasePath)) {
    $DatabasePath = "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB"
    Write-Host "Usando caminho padrão do banco: $DatabasePath" -ForegroundColor Yellow
}

# Caminho do isql
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

# Verificar se o isql existe
if (-not (Test-Path $IsqlPath)) {
    # Tenta buscar em outros locais
    $found = Get-ChildItem -Path "C:\Program Files" -Filter "isql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $IsqlPath = $found.FullName
        Write-Host "Firebird ISQL encontrado em: $IsqlPath" -ForegroundColor Green
    } else {
        Write-Host "ERRO: isql.exe não encontrado em: $FirebirdPath" -ForegroundColor Red
        Write-Host "Verifique se o Firebird está instalado corretamente." -ForegroundColor Yellow
        exit 1
    }
}

# Caminho do script SQL
$ScriptPath = Join-Path $PSScriptRoot "verificar_cbo_cid_competencia.sql"

# Verificar se o script existe
if (-not (Test-Path $ScriptPath)) {
    Write-Host "ERRO: Script SQL não encontrado: $ScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Verificação: CBO, CID e Descrições por Competência" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host "Script: $ScriptPath" -ForegroundColor Cyan
Write-Host ""

# Executar o script SQL
$sqlCommands = Get-Content $ScriptPath -Raw

# Criar arquivo temporário com os comandos SQL
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII

try {
    # Executar isql
    Write-Host "Executando consultas SQL..." -ForegroundColor Yellow
    Write-Host "---" -ForegroundColor Gray
    Write-Host ""
    
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    # Exibir resultado
    Write-Host $result
    
    # Salvar resultado em arquivo
    $outputFile = Join-Path $PSScriptRoot "resultado_verificacao_cbo_cid_competencia.txt"
    $result | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host ""
    Write-Host "Resultado salvo em: $outputFile" -ForegroundColor Green
}
catch {
    Write-Host "ERRO ao executar o script: $_" -ForegroundColor Red
}
finally {
    # Remover arquivo temporário
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "CONCLUSÃO:" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Baseado na estrutura do banco:" -ForegroundColor White
Write-Host "- TB_CID e TB_OCUPACAO NÃO possuem DT_COMPETENCIA" -ForegroundColor Yellow
Write-Host "  → CIDs e CBOs valem para TODAS as competências" -ForegroundColor Yellow
Write-Host ""
Write-Host "- RL_PROCEDIMENTO_CID e RL_PROCEDIMENTO_OCUPACAO POSSUEM DT_COMPETENCIA" -ForegroundColor Yellow
Write-Host "  → Os RELACIONAMENTOS entre procedimentos e CIDs/CBOs variam por competência" -ForegroundColor Yellow
Write-Host ""
Write-Host "- As descrições (NO_CID, NO_OCUPACAO) nas tabelas principais são únicas" -ForegroundColor Yellow
Write-Host "- As descrições nas tabelas relacionais podem variar (denormalizadas)" -ForegroundColor Yellow
Write-Host ""
