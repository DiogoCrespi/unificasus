# Script PowerShell para DELETAR duplicatas de CID10

# Configurações
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$User = "SYSDBA"
$Password = "masterkey"

# Tenta ler o caminho do banco
$configFile = "C:\Program Files\claupers\unificasus\unificasus.ini"
$DatabasePath = ""

if (Test-Path $configFile) {
    $configContent = Get-Content $configFile -Raw
    if ($configContent -match '(?m)^local\s*=\s*(.+)$') {
        $DatabasePath = $matches[1].Trim()
    }
}

if ([string]::IsNullOrEmpty($DatabasePath)) {
    $DatabasePath = "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB"
}

# Caminho do isql
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

if (-not (Test-Path $IsqlPath)) {
    $found = Get-ChildItem -Path "C:\Program Files" -Filter "isql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $IsqlPath = $found.FullName
    } else {
        Write-Host "ERRO: isql.exe nao encontrado" -ForegroundColor Red
        exit 1
    }
}

# Usar versão com logs detalhados
$ScriptPath = Join-Path $PSScriptRoot "deletar_duplicatas_com_logs.sql"
$ScriptPathAlternativo = Join-Path $PSScriptRoot "deletar_duplicatas_simples.sql"

# Verificar se o script com logs existe, senão usar o alternativo
if (-not (Test-Path $ScriptPath)) {
    Write-Host "AVISO: Script com logs nao encontrado, usando versao simples..." -ForegroundColor Yellow
    $ScriptPath = $ScriptPathAlternativo
    if (-not (Test-Path $ScriptPath)) {
        Write-Host "ERRO: Nenhum script SQL encontrado!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "DELETAR Duplicatas de CID10" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Estrategia: Mantem apenas o registro com menor INDICE de cada grupo" -ForegroundColor Yellow
Write-Host "(CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA)" -ForegroundColor Yellow
Write-Host ""

# Executar script SQL
$sqlCommands = Get-Content $ScriptPath -Raw
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII

$startTime = Get-Date

try {
    $timestampInicio = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestampInicio] Iniciando execucao..." -ForegroundColor Cyan
    Write-Host "[$timestampInicio] Executando delecao (pode demorar alguns minutos)..." -ForegroundColor Yellow
    Write-Host ""
    
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    $timestampFim = Get-Date -Format "HH:mm:ss"
    
    # Verificar se houve erro
    $hasError = $false
    
    # Mostrar resultados com timestamps
    $result | Where-Object { 
        $_ -notmatch '^Database:|^User:|^SQL>|^CON>' -and 
        $_ -match '\S'
    } | ForEach-Object { 
        $line = $_.Trim()
        $timestamp = Get-Date -Format "HH:mm:ss"
        
        if ($line -match 'ERROR|ERRO|failed|Token unknown|exception|Exception') {
            Write-Host "[$timestamp] $line" -ForegroundColor Red
            $hasError = $true
        } elseif ($line -match '===.*===') {
            # Separadores de seção
            Write-Host ""
            Write-Host "[$timestamp] $line" -ForegroundColor Magenta
        } elseif ($line -match 'ANTES|DEPOIS|INICIANDO|CONCLUÍDA|RESUMO|SUCESSO|AVISO') {
            Write-Host "[$timestamp] $line" -ForegroundColor Cyan
        } elseif ($line -match 'STATUS|TOTAL|REGISTROS|DUPLICATAS|UNICOS|GRUPOS|ESTIMATIVA|DETALHE|RESULTADO') {
            Write-Host "[$timestamp]   $line" -ForegroundColor Yellow
        } elseif ($line -match '^\d+$' -or $line -match '^\d+\s+\d+\s+\d+' -or $line -match '^\d+\s+\d+') {
            # Linhas com números (resultados de SELECT)
            Write-Host "[$timestamp]   $line" -ForegroundColor White
        } else {
            Write-Host "[$timestamp] $line" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor $(if ($hasError) { "Red" } else { "Green" })
    if ($hasError) {
        Write-Host "[$timestampFim] ERRO durante a execucao!" -ForegroundColor Red
    } else {
        Write-Host "[$timestampFim] Delecao concluida!" -ForegroundColor Green
    }
    
    $minutos = [math]::Floor($duration.TotalMinutes)
    $segundos = [math]::Round($duration.TotalSeconds % 60, 2)
    if ($minutos -gt 0) {
        Write-Host "[$timestampFim] Tempo decorrido: $minutos minuto(s) e $segundos segundo(s)" -ForegroundColor $(if ($hasError) { "Red" } else { "Green" })
    } else {
        Write-Host "[$timestampFim] Tempo decorrido: $segundos segundo(s)" -ForegroundColor $(if ($hasError) { "Red" } else { "Green" })
    }
    Write-Host "[$timestampFim] Inicio: $timestampInicio | Fim: $timestampFim" -ForegroundColor Gray
    Write-Host "============================================================================" -ForegroundColor $(if ($hasError) { "Red" } else { "Green" })
}
catch {
    Write-Host "ERRO: $_" -ForegroundColor Red
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

