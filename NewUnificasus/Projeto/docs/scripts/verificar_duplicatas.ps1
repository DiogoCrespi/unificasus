# Script para verificar duplicatas de CID10

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

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Verificando Duplicatas de CID10" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Função para executar query
function Execute-Query {
    param([string]$SqlFile, [string]$Description)
    
    Write-Host "[$Description]..." -ForegroundColor Yellow
    
    $sqlCommands = Get-Content $SqlFile -Raw
    $tempFile = [System.IO.Path]::GetTempFileName()
    $sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII
    
    try {
        $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
        
        $result | Where-Object { 
            $_ -notmatch '^Database:|^User:|^SQL>|^CON>' -and 
            $_ -match '\S'
        } | ForEach-Object { 
            Write-Host $_ -ForegroundColor White
        }
    }
    catch {
        Write-Host "ERRO: $_" -ForegroundColor Red
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
    
    Write-Host ""
}

# Verificar C73
$scriptDir = $PSScriptRoot
Execute-Query -SqlFile (Join-Path $scriptDir "verificar_duplicatas_c73_todos.sql") -Description "1/2 Verificando duplicatas do C73"

# Verificar duplicatas em geral
Execute-Query -SqlFile (Join-Path $scriptDir "verificar_duplicatas_geral.sql") -Description "2/2 Verificando duplicatas em geral"

