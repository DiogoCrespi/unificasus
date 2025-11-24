# Script PowerShell para testar deleção de duplicatas de CID10
# Testa primeiro com C73 no procedimento 0201010038

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
    }
}

if ([string]::IsNullOrEmpty($DatabasePath)) {
    $DatabasePath = "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB"
}

# Caminho do isql
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

# Verificar se o isql existe
if (-not (Test-Path $IsqlPath)) {
    $found = Get-ChildItem -Path "C:\Program Files" -Filter "isql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $IsqlPath = $found.FullName
    } else {
        Write-Host "ERRO: isql.exe nao encontrado" -ForegroundColor Red
        exit 1
    }
}

$ScriptPath = Join-Path $PSScriptRoot "deletar_duplicatas_cid_teste.sql"

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "TESTE: Deletar Duplicatas CID10 - C73 no procedimento 0201010038" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host ""

# Executar script SQL (apenas visualização primeiro)
$sqlCommands = Get-Content $ScriptPath -Raw
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII

try {
    Write-Host "Executando verificacao (sem deletar ainda)..." -ForegroundColor Yellow
    Write-Host ""
    
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    # Mostrar resultados
    $result | Where-Object { 
        $_ -notmatch '^Database:|^User:|^SQL>|^CON>' -and 
        $_ -match '\S'
    } | ForEach-Object { 
        Write-Host $_ -ForegroundColor White 
    }
    
    Write-Host ""
    Write-Host "============================================================================" -ForegroundColor Yellow
    Write-Host "ATENCAO: O script SQL esta configurado apenas para VISUALIZAR" -ForegroundColor Yellow
    Write-Host "Para DELETAR, edite o arquivo deletar_duplicatas_cid_teste.sql" -ForegroundColor Yellow
    Write-Host "e descomente a secao DELETE (linha 4)" -ForegroundColor Yellow
    Write-Host "============================================================================" -ForegroundColor Yellow
}
catch {
    Write-Host "ERRO: $_" -ForegroundColor Red
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

