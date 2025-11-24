# Script para copiar arquivos para o servidor
# Caminho do banco: E:\claupers\unificasus\

param(
    [string]$ServerHost = "192.168.0.3",
    [string]$ServerPath = "E:\claupers\unificasus",
    [string]$ServerUser = "AFSc\dvcrespi",
    [string]$ServerPassword = "Ufetly20#"
)

$scriptsPath = $PSScriptRoot
$uncPath = "\\$ServerHost\E$\claupers\unificasus"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Copiando arquivos para o servidor" -ForegroundColor Cyan
Write-Host "Servidor: $ServerHost" -ForegroundColor Cyan
Write-Host "Destino: $uncPath" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Arquivos para copiar
$files = @(
    "limpar_duplicatas_com_logs_detalhados.sql",
    "executar_limpeza_servidor.bat",
    "check_duplicatas.sql",
    "listar_duplicatas_antes.sql"
)

$successCount = 0
$errorCount = 0

foreach ($file in $files) {
    $localFile = Join-Path $scriptsPath $file
    $destFile = Join-Path $uncPath $file
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Copiando $file..." -ForegroundColor Yellow
    
    if (-not (Test-Path $localFile)) {
        Write-Host "  [ERRO] Arquivo não encontrado: $localFile" -ForegroundColor Red
        $errorCount++
        continue
    }
    
    try {
        # Criar credenciais para acesso à rede
        $securePassword = ConvertTo-SecureString $ServerPassword -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential($ServerUser, $securePassword)
        
        # Mapear unidade de rede temporariamente
        $driveLetter = "Z:"
        $mappedDrive = $null
        
        # Verificar se já existe mapeamento
        $existingDrive = Get-PSDrive -Name ($driveLetter.TrimEnd(':')) -ErrorAction SilentlyContinue
        if ($existingDrive) {
            Remove-PSDrive -Name ($driveLetter.TrimEnd(':')) -Force -ErrorAction SilentlyContinue
        }
        
        Write-Host "  [INFO] Mapeando unidade de rede..." -ForegroundColor Gray
        $mappedDrive = New-PSDrive -Name ($driveLetter.TrimEnd(':')) -PSProvider FileSystem -Root "\\$ServerHost\E$" -Credential $credential -Persist:$false
        
        if ($mappedDrive) {
            $mappedPath = Join-Path $driveLetter "claupers\unificasus"
            
            # Verificar se o caminho existe
            if (-not (Test-Path $mappedPath)) {
                Write-Host "  [AVISO] Criando diretório: $mappedPath" -ForegroundColor Yellow
                New-Item -ItemType Directory -Path $mappedPath -Force -ErrorAction Stop | Out-Null
            }
            
            $destFileMapped = Join-Path $mappedPath $file
            Copy-Item -Path $localFile -Destination $destFileMapped -Force -ErrorAction Stop
            Write-Host "  [OK] $file copiado com sucesso!" -ForegroundColor Green
            Write-Host "       Destino: $destFileMapped" -ForegroundColor Gray
            $successCount++
            
            # Desmapear unidade
            Remove-PSDrive -Name ($driveLetter.TrimEnd(':')) -Force -ErrorAction SilentlyContinue
        } else {
            throw "Falha ao mapear unidade de rede"
        }
    }
    catch {
        Write-Host "  [ERRO] Falha ao copiar $file" -ForegroundColor Red
        Write-Host "         Erro: $_" -ForegroundColor Red
        $errorCount++
        
        # Tentar desmapear em caso de erro
        Remove-PSDrive -Name ($driveLetter.TrimEnd(':')) -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Resumo:" -ForegroundColor Cyan
Write-Host "  Sucesso: $successCount arquivos" -ForegroundColor Green
Write-Host "  Erros: $errorCount arquivos" -ForegroundColor $(if ($errorCount -eq 0) { "Green" } else { "Red" })
Write-Host "========================================" -ForegroundColor Cyan

if ($successCount -gt 0) {
    Write-Host ""
    Write-Host "[INFO] Para executar no servidor:" -ForegroundColor Yellow
    Write-Host "  1. Conecte via RDP: mstsc /v:$ServerHost" -ForegroundColor Cyan
    Write-Host "  2. Execute: $ServerPath\executar_limpeza_servidor.bat" -ForegroundColor Cyan
    Write-Host ""
}

if ($errorCount -gt 0) {
    Write-Host "[AVISO] Alguns arquivos não foram copiados." -ForegroundColor Yellow
    Write-Host "        Verifique as permissões de rede ou copie manualmente." -ForegroundColor Yellow
    exit 1
}

exit 0

