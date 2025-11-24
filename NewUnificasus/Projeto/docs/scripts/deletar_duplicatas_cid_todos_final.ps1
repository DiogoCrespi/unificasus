# Script PowerShell para DELETAR TODAS as duplicatas de CID10
# Preserva a melhor descrição (MAIÚSCULAS quando disponível)

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

$ScriptPath = Join-Path $PSScriptRoot "deletar_duplicatas_cid_simples_final.sql"

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "DELETAR TODAS as Duplicatas de CID10" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Estrategia: Mantem o primeiro registro (menor INDICE) de cada grupo" -ForegroundColor Yellow
Write-Host "Total estimado de duplicatas: ~106.268" -ForegroundColor Yellow
Write-Host ""

# Confirmar antes de deletar
Write-Host "ATENCAO: Este script vai DELETAR TODAS as duplicatas!" -ForegroundColor Red
Write-Host ""
$confirm = Read-Host "Deseja continuar? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operacao cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Executando delecao (pode demorar alguns minutos)..." -ForegroundColor Yellow
Write-Host ""

# Executar script SQL
$sqlCommands = Get-Content $ScriptPath -Raw
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII

$startTime = Get-Date

try {
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    # Mostrar resultados
    $result | Where-Object { 
        $_ -notmatch '^Database:|^User:|^SQL>|^CON>' -and 
        $_ -match '\S'
    } | ForEach-Object { 
        if ($_ -match 'ERROR|ERRO|failed|Token unknown') {
            Write-Host $_ -ForegroundColor Red
        } elseif ($_ -match 'DEPOIS|ANTES|STATUS|TOTAL|REGISTROS|DUPLICATAS') {
            Write-Host $_ -ForegroundColor Cyan
        } else {
            Write-Host $_ -ForegroundColor White
        }
    }
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Green
    Write-Host "Delecao concluida!" -ForegroundColor Green
    Write-Host "Tempo decorrido: $($duration.TotalSeconds) segundos" -ForegroundColor Green
    Write-Host "============================================================================" -ForegroundColor Green
}
catch {
    Write-Host "ERRO: $_" -ForegroundColor Red
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

